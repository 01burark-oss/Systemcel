using System.Net;
using System.Text.Json;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CashTracker.Tests;

public sealed class FaturaMusteriOnayServiceTests
{
    [Fact]
    public async Task SendAndRespond_StoresOnlyHashedTokenAndCorrectionAudit()
    {
        await using var fixture = await ApprovalFixture.CreateAsync();

        var sent = await fixture.Service.SendAsync(fixture.InvoiceId);

        Assert.Equal(FaturaMusteriOnayDurumlari.Bekliyor, sent.Durum);
        Assert.Equal("0532 *** ** 67", sent.AliciTelefonMaskeli);
        Assert.Equal("5321234567", fixture.Sender.LastPhone);
        Assert.Contains("resmi e-belge onayı değildir", fixture.Sender.LastMessage, StringComparison.OrdinalIgnoreCase);

        var token = new Uri(sent.OnayUrl).Segments[^1];
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var row = await db.FaturaMusteriOnaylari.SingleAsync();
            Assert.Equal(64, row.TokenHash.Length);
            Assert.DoesNotContain(token, row.TokenHash, StringComparison.Ordinal);
            Assert.DoesNotContain(token, JsonSerializer.Serialize(row), StringComparison.Ordinal);
        }

        var publicView = await fixture.Service.GetPublicAsync(token);
        Assert.NotNull(publicView);
        Assert.Equal("12******90", publicView!.CariVergiNoMaskeli);
        Assert.Contains("resmi e-belge onayı değildir", publicView.Aciklama, StringComparison.OrdinalIgnoreCase);

        var responded = await fixture.Service.RespondAsync(
            token,
            new PublicFaturaMusteriOnayYaniti { BilgilerDogru = false, Aciklama = "Adresimiz değişti." },
            "127.0.0.1",
            "test-agent");

        Assert.Equal(FaturaMusteriOnayDurumlari.DuzeltmeIstendi, responded!.Durum);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var row = await db.FaturaMusteriOnaylari.SingleAsync();
            Assert.Equal("Adresimiz değişti.", row.YanitNotu);
            Assert.Equal(64, row.IstemciIpHash.Length);
            Assert.Equal(64, row.UserAgentHash.Length);
        }
    }

    [Fact]
    public async Task SendAsync_RejectsRapidResendAndTenantLeakage()
    {
        await using var fixture = await ApprovalFixture.CreateAsync();
        await fixture.Service.SendAsync(fixture.InvoiceId);

        var cooldown = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.SendAsync(fixture.InvoiceId));
        Assert.Contains("15 dakika", cooldown.Message);

        fixture.Business.Active = new Isletme { Id = 2, Ad = "Başka işletme", IsAktif = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.GetLatestAsync(fixture.InvoiceId));
    }

    [Fact]
    public async Task NetgsmSender_UsesOfficialRestV2Contract()
    {
        HttpRequestMessage? captured = null;
        string? requestBody = null;
        var handler = new StubHttpHandler(async request =>
        {
            captured = request;
            requestBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"code\":\"00\",\"description\":\"Başarılı\",\"jobid\":\"12345\"}")
            };
        });
        var settings = ConfiguredSettings();
        var sender = new NetgsmMusteriSmsSender(new HttpClient(handler), settings, NullLogger<NetgsmMusteriSmsSender>.Instance);

        var result = await sender.SendAsync("5321234567", "Teyit bağlantısı");

        Assert.True(result.Basarili);
        Assert.Equal("12345", result.IslemId);
        Assert.Equal("https://api.netgsm.com.tr/sms/rest/v2/send", captured!.RequestUri!.ToString());
        Assert.Equal("Basic", captured.Headers.Authorization!.Scheme);
        using var payload = JsonDocument.Parse(requestBody!);
        Assert.Equal("SYSTEMCEL", payload.RootElement.GetProperty("msgheader").GetString());
        Assert.Equal("0", payload.RootElement.GetProperty("iysfilter").GetString());
        Assert.Equal("TR", payload.RootElement.GetProperty("encoding").GetString());
        Assert.Equal("5321234567", payload.RootElement.GetProperty("messages")[0].GetProperty("no").GetString());
    }

    private static MusteriSmsSettings ConfiguredSettings() => new()
    {
        Username = "test-user",
        Password = "test-password",
        Header = "SYSTEMCEL",
        PublicBaseUrl = "https://systemcel.app",
        LinkExpiryHours = 72,
        ResendCooldownMinutes = 15
    };

    private sealed class ApprovalFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private ApprovalFixture(
            SqliteConnection connection,
            SingleDbContextFactory factory,
            FakeIsletmeService business,
            RecordingSmsSender sender,
            FaturaMusteriOnayService service,
            int invoiceId)
        {
            _connection = connection;
            Factory = factory;
            Business = business;
            Sender = sender;
            Service = service;
            InvoiceId = invoiceId;
        }

        public SingleDbContextFactory Factory { get; }
        public FakeIsletmeService Business { get; }
        public RecordingSmsSender Sender { get; }
        public FaturaMusteriOnayService Service { get; }
        public int InvoiceId { get; }

        public static async Task<ApprovalFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                .UseSqlite(connection)
                .Options;
            var factory = new SingleDbContextFactory(options);
            var business = new FakeIsletmeService
            {
                Active = new Isletme { Id = 1, Ad = "Örnek İşletme", IsAktif = true }
            };
            int invoiceId;
            await using (var db = factory.CreateDbContext())
            {
                await db.Database.EnsureCreatedAsync();
                db.Isletmeler.Add(business.Active);
                var customer = new CariKart
                {
                    IsletmeId = 1,
                    Tip = "Musteri",
                    Unvan = "Örnek Müşteri A.Ş.",
                    Telefon = "+90 532 123 45 67",
                    VergiNoTc = "1234567890",
                    Adres = "Örnek Mah. No: 1",
                    Aktif = true
                };
                db.CariKartlari.Add(customer);
                await db.SaveChangesAsync();
                var invoice = new Fatura
                {
                    IsletmeId = 1,
                    CariKartId = customer.Id,
                    FaturaTipi = "Satis",
                    Durum = FaturaDurum.YerelTaslak,
                    YerelFaturaNo = "FAT-2026-001",
                    GenelToplam = 1_250m,
                    Tarih = new DateTime(2026, 8, 22),
                    CreatedAt = new DateTime(2026, 8, 22, 9, 0, 0),
                    UpdatedAt = new DateTime(2026, 8, 22, 9, 0, 0)
                };
                db.Faturalar.Add(invoice);
                await db.SaveChangesAsync();
                invoiceId = invoice.Id;
            }

            var sender = new RecordingSmsSender();
            var service = new FaturaMusteriOnayService(factory, business, sender, ConfiguredSettings());
            return new ApprovalFixture(connection, factory, business, sender, service, invoiceId);
        }

        public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
    }

    private sealed class RecordingSmsSender : IMusteriSmsSender
    {
        public bool IsConfigured => true;
        public string LastPhone { get; private set; } = string.Empty;
        public string LastMessage { get; private set; } = string.Empty;

        public Task<MusteriSmsGonderimSonucu> SendAsync(string phoneNumber, string message, CancellationToken ct = default)
        {
            LastPhone = phoneNumber;
            LastMessage = message;
            return Task.FromResult(new MusteriSmsGonderimSonucu(true, "Test", "job-1", string.Empty));
        }
    }

    private sealed class StubHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => callback(request);
    }
}
