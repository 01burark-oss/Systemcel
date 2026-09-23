using System.Net;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class TelegramBildirimBaglantiTests
{
    [Fact]
    public async Task Pairing_IsScopedToActiveBusinessMemberAndDoesNotOverwriteAnotherUser()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite(connection).Options;
        var factory = new SingleDbContextFactory(options);
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.EnsureCreatedAsync();
            db.Isletmeler.AddRange(new Isletme { Id = 1, Ad = "A" }, new Isletme { Id = 2, Ad = "B" });
            db.Kullanicilar.AddRange(
                new Kullanici { Id = 1, AuthProviderUserId = "user-a", Eposta = "a@test.local", Durum = "Aktif" },
                new Kullanici { Id = 2, AuthProviderUserId = "user-b", Eposta = "b@test.local", Durum = "Aktif" });
            db.IsletmeUyelikleri.AddRange(
                new IsletmeUyelik { IsletmeId = 1, KullaniciId = 1, Rol = "isletme_sahibi", Durum = "Aktif" },
                new IsletmeUyelik { IsletmeId = 2, KullaniciId = 2, Rol = "isletme_sahibi", Durum = "Aktif" });
            await db.SaveChangesAsync();
        }

        var pairing = new TelegramBildirimBaglantiService(factory);
        var firstCode = await pairing.EnsureCodeAsync(1, "user-a");
        var secondCode = await pairing.EnsureCodeAsync(2, "user-b");
        Assert.NotEqual(firstCode.Code, secondCode.Code);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => pairing.EnsureCodeAsync(2, "user-a"));
        Assert.False(await pairing.TryCompleteAsync(firstCode.Code, -10, 10));
        Assert.False(await pairing.TryCompleteAsync(firstCode.Code, 10, 11));
        Assert.True(await pairing.TryCompleteAsync(firstCode.Code, 10, 10));
        Assert.True(await pairing.TryCompleteAsync(secondCode.Code, 20, 20));
        Assert.False(await pairing.TryCompleteAsync(firstCode.Code, 30, 30));
        Assert.Equal("10", (await pairing.GetStateAsync(1, "user-a")).ChatId);
        Assert.Equal("20", (await pairing.GetStateAsync(2, "user-b")).ChatId);

        var handler = new CapturingHandler();
        var telegramSettings = new TelegramSettings { BotToken = "test-token" };
        var bot = new TelegramBotService(new HttpClient(handler), telegramSettings);
        var adapter = new TelegramBildirimAdapter(factory, bot, telegramSettings);
        var payload = "{\"Baslik\":\"Vade geçti\",\"Mesaj\":\"Ödeme bekliyor\",\"Url\":\"/app/faturalar\"}";
        await adapter.SendAsync(new(1, 1, "user-a", BildirimKanallari.Telegram, payload, "claim", 0));
        Assert.Contains("chat_id=10", handler.LastBody);
        Assert.DoesNotContain("chat_id=20", handler.LastBody);
        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.SendAsync(
            new(2, 2, "user-a", BildirimKanallari.Telegram, payload, "claim", 0)));

        await pairing.ClearAsync(1, "user-a");
        Assert.False((await pairing.GetStateAsync(1, "user-a")).Bagli);
        Assert.True((await pairing.GetStateAsync(2, "user-b")).Bagli);
        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.SendAsync(
            new(3, 1, "user-a", BildirimKanallari.Telegram, payload, "claim", 0)));
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"ok\":true}") };
        }
    }
}
