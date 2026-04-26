using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CashTracker.App.Services
{
    internal sealed class GelirGiderLocalApiServer : IAsyncDisposable
    {
        private readonly IKasaService _kasaService;
        private readonly IIsletmeService _isletmeService;
        private readonly IKalemTanimiService _kalemTanimiService;
        private readonly IUrunHizmetService _urunHizmetService;
        private readonly IStokService _stokService;
        private WebApplication? _app;

        public GelirGiderLocalApiServer(
            IKasaService kasaService,
            IIsletmeService isletmeService,
            IKalemTanimiService kalemTanimiService,
            IUrunHizmetService urunHizmetService,
            IStokService stokService)
        {
            _kasaService = kasaService;
            _isletmeService = isletmeService;
            _kalemTanimiService = kalemTanimiService;
            _urunHizmetService = urunHizmetService;
            _stokService = stokService;
        }

        public Uri BaseUri { get; private set; } = null!;

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_app is not null)
                return;

            Exception? lastError = null;
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var port = GetAvailablePort();
                try
                {
                    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
                    {
                        Args = Array.Empty<string>(),
                        ApplicationName = typeof(GelirGiderLocalApiServer).Assembly.FullName,
                        ContentRootPath = AppContext.BaseDirectory
                    });
                    builder.Logging.ClearProviders();
                    builder.WebHost.UseKestrel().UseUrls($"http://127.0.0.1:{port}");

                    var app = builder.Build();
                    MapApi(app);
                    MapReactFiles(app);
                    await app.StartAsync(cancellationToken);

                    _app = app;
                    BaseUri = new Uri($"http://127.0.0.1:{port}/");
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            throw new InvalidOperationException("Gelir/Gider ekran servisi baslatilamadi.", lastError);
        }

        public async ValueTask DisposeAsync()
        {
            if (_app is null)
                return;

            try
            {
                await _app.StopAsync(TimeSpan.FromSeconds(2));
            }
            finally
            {
                await _app.DisposeAsync();
                _app = null;
            }
        }

        private void MapApi(WebApplication app)
        {
            app.MapGet("/api/ekran/gelir-gider", async () =>
            {
                try
                {
                    return Results.Ok(await BuildScreenAsync());
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Ekran verileri yuklenemedi: {ex.Message}");
                }
            });

            app.MapPost("/api/ekran/gelir-gider/kayitlar", async (KayitKaydetIstek request) =>
            {
                var validation = ValidateRequest(request, isUpdate: false);
                if (validation is not null)
                    return Results.BadRequest(validation);

                try
                {
                    var domain = ToDomainRecord(request);
                    var kayitId = await _kasaService.CreateAsync(domain);
                    if (request.stokGiris?.aktif == true)
                        await CreateStockMovementOrRollbackAsync(kayitId, request);

                    return Results.Ok(new ApiMesaj("Kayıt eklendi."));
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new ApiHata($"Kayıt eklenemedi: {ex.Message}"));
                }
            });

            app.MapPut("/api/ekran/gelir-gider/kayitlar/{id:int}", async (int id, KayitKaydetIstek request) =>
            {
                request.id = id;
                var validation = ValidateRequest(request, isUpdate: true);
                if (validation is not null)
                    return Results.BadRequest(validation);

                try
                {
                    await _kasaService.UpdateAsync(ToDomainRecord(request));
                    return Results.Ok(new ApiMesaj("Kayıt güncellendi."));
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new ApiHata($"Kayıt güncellenemedi: {ex.Message}"));
                }
            });

            app.MapDelete("/api/ekran/gelir-gider/kayitlar/{id:int}", async (int id) =>
            {
                try
                {
                    await _kasaService.DeleteAsync(id);
                    return Results.Ok(new ApiMesaj("Kayıt silindi."));
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new ApiHata($"Kayıt silinemedi: {ex.Message}"));
                }
            });
        }

        private static void MapReactFiles(WebApplication app)
        {
            var distPath = Path.Combine(AppContext.BaseDirectory, "ClientApp", "dist");
            var indexPath = Path.Combine(distPath, "index.html");
            if (!Directory.Exists(distPath) || !File.Exists(indexPath))
            {
                app.MapFallback(() => Results.Content(
                    "<!doctype html><html><head><meta charset=\"utf-8\"><title>Gelir/Gider</title></head><body style=\"font-family:Segoe UI,sans-serif;padding:32px;background:#ecf1f8;color:#172234\"><h1>React ekranı hazır değil</h1><p>CashTracker.App/ClientApp içinde npm install ve npm run build çalıştırın.</p></body></html>",
                    "text/html; charset=utf-8"));
                return;
            }

            var fileProvider = new PhysicalFileProvider(distPath);
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
            app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
            app.MapFallback(async context =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.SendFileAsync(indexPath);
            });
        }

        private async Task<GelirGiderEkranDto> BuildScreenAsync()
        {
            var activeBusiness = await _isletmeService.GetActiveAsync();
            var records = (await _kasaService.GetAllAsync())
                .Select(ToDto)
                .ToList();
            var incomeCategories = await GetCategoriesAsync("Gelir");
            var expenseCategories = await GetCategoriesAsync("Gider");
            var products = (await _urunHizmetService.GetAllAsync())
                .Where(x => x.Aktif && string.Equals(x.Tip, "Urun", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Ad, StringComparer.OrdinalIgnoreCase)
                .Select(x => new StokUrunDto(x.Id, x.Ad, x.Birim))
                .ToList();

            return new GelirGiderEkranDto
            {
                aktifIsletme = string.IsNullOrWhiteSpace(activeBusiness.Ad) ? "Bilinmiyor" : activeBusiness.Ad.Trim(),
                kayitlar = records,
                gelirKalemleri = incomeCategories,
                giderKalemleri = expenseCategories,
                stokUrunleri = products,
                odemeYontemleri = new List<OdemeYontemiDto>
                {
                    new("nakit", "Nakit"),
                    new("krediKarti", "Kredi Kartı"),
                    new("onlineOdeme", "Online Ödeme"),
                    new("havale", "Havale")
                }
            };
        }

        private async Task<List<string>> GetCategoriesAsync(string tur)
        {
            return (await _kalemTanimiService.GetByTipAsync(tur))
                .Select(x => x.Ad?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Select(x => x!)
                .ToList();
        }

        private async Task CreateStockMovementOrRollbackAsync(int kayitId, KayitKaydetIstek request)
        {
            try
            {
                var stock = request.stokGiris;
                if (stock is null || !stock.aktif)
                    return;

                var product = await _urunHizmetService.GetByIdAsync(stock.urunId);
                if (product is null || !product.Aktif || !string.Equals(product.Tip, "Urun", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Stok girişi için geçerli ürün seçin.");

                await _stokService.CreateMovementAsync(new StokHareketCreateRequest
                {
                    UrunHizmetId = stock.urunId,
                    Tarih = ParseDate(request.tarih),
                    Miktar = stock.miktar,
                    Kaynak = "KasaGider",
                    Aciklama = $"Gider stok girişi | Kayıt: {kayitId} | {product.Ad}"
                });
            }
            catch
            {
                await _kasaService.DeleteAsync(kayitId);
                throw;
            }
        }

        private static ApiHata? ValidateRequest(KayitKaydetIstek request, bool isUpdate)
        {
            if (request is null)
                return new ApiHata("Kayıt bilgileri alınamadı.");

            if (isUpdate && request.stokGiris?.aktif == true)
                return new ApiHata("Düzenlenen kayıtta stok girişi yapılamaz. Yeni gider kaydı açın.");

            var tur = NormalizeTur(request.tur);
            if (tur is not ("gelir" or "gider"))
                return new ApiHata("Tür alanı gelir veya gider olmalıdır.");

            if (string.IsNullOrWhiteSpace(request.kalem))
                return new ApiHata("Kalem seçin.");

            if (request.tutar <= 0)
                return new ApiHata("Tutar sıfırdan büyük olmalıdır.");

            if (!TryParseDate(request.tarih, out _))
                return new ApiHata("Tarih geçerli değil.");

            if (request.stokGiris?.aktif == true)
            {
                if (tur != "gider")
                    return new ApiHata("Stok girişi sadece gider kaydı için kullanılır.");

                if (request.stokGiris.urunId <= 0)
                    return new ApiHata("Stok girişi için ürün seçin.");

                if (request.stokGiris.miktar <= 0)
                    return new ApiHata("Stok miktarı sıfırdan büyük olmalıdır.");
            }

            return null;
        }

        private static Kasa ToDomainRecord(KayitKaydetIstek request)
        {
            var tur = NormalizeTur(request.tur);
            var kalem = (request.kalem ?? string.Empty).Trim();
            return new Kasa
            {
                Id = request.id ?? 0,
                Tarih = ParseDate(request.tarih),
                Tip = tur == "gider" ? "Gider" : "Gelir",
                Tutar = request.tutar,
                OdemeYontemi = ToDomainPayment(request.odemeYontemi),
                Kalem = kalem,
                GiderTuru = tur == "gider" ? kalem : null,
                Aciklama = string.IsNullOrWhiteSpace(request.aciklama) ? null : request.aciklama.Trim()
            };
        }

        private static KayitDto ToDto(Kasa row)
        {
            var tur = ToApiTur(row.Tip);
            return new KayitDto
            {
                id = row.Id,
                tarih = row.Tarih.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
                tur = tur,
                tutar = row.Tutar,
                odemeYontemi = ToApiPayment(row.OdemeYontemi),
                kalem = (row.Kalem ?? row.GiderTuru ?? string.Empty).Trim(),
                aciklama = row.Aciklama ?? string.Empty
            };
        }

        private static DateTime ParseDate(string? value)
        {
            return TryParseDate(value, out var result) ? result : DateTime.Now;
        }

        private static bool TryParseDate(string? value, out DateTime result)
        {
            var raw = (value ?? string.Empty).Trim();
            return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result) ||
                   DateTime.TryParse(raw, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AssumeLocal, out result);
        }

        private static string NormalizeTur(string? value)
        {
            return NormalizeAscii(value) switch
            {
                "gider" or "expense" => "gider",
                "gelir" or "income" => "gelir",
                _ => string.Empty
            };
        }

        private static string ToApiTur(string? value)
        {
            return NormalizeAscii(value) is "gider" or "cikis" or "expense"
                ? "gider"
                : "gelir";
        }

        private static string ToDomainPayment(string? value)
        {
            return NormalizeAscii(value) switch
            {
                "kredikarti" or "kredi karti" or "kart" => "KrediKarti",
                "onlineodeme" or "online odeme" or "online" => "OnlineOdeme",
                "havale" or "transfer" => "Havale",
                _ => "Nakit"
            };
        }

        private static string ToApiPayment(string? value)
        {
            return NormalizeAscii(value) switch
            {
                "kredikarti" or "kredi karti" or "kart" => "krediKarti",
                "onlineodeme" or "online odeme" or "online" => "onlineOdeme",
                "havale" or "transfer" => "havale",
                _ => "nakit"
            };
        }

        private static string NormalizeAscii(string? value)
        {
            return (value ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Replace('\u0131', 'i')
                .Replace('\u015f', 's')
                .Replace('\u011f', 'g')
                .Replace('\u00fc', 'u')
                .Replace('\u00f6', 'o')
                .Replace('\u00e7', 'c');
        }

        private static int GetAvailablePort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public sealed record ApiHata(string mesaj);
        public sealed record ApiMesaj(string mesaj);
        public sealed record OdemeYontemiDto(string deger, string etiket);
        public sealed record StokUrunDto(int id, string ad, string birim);

        public sealed class GelirGiderEkranDto
        {
            public string aktifIsletme { get; set; } = string.Empty;
            public List<KayitDto> kayitlar { get; set; } = new();
            public List<string> gelirKalemleri { get; set; } = new();
            public List<string> giderKalemleri { get; set; } = new();
            public List<StokUrunDto> stokUrunleri { get; set; } = new();
            public List<OdemeYontemiDto> odemeYontemleri { get; set; } = new();
        }

        public sealed class KayitDto
        {
            public int id { get; set; }
            public string tarih { get; set; } = string.Empty;
            public string tur { get; set; } = "gelir";
            public decimal tutar { get; set; }
            public string odemeYontemi { get; set; } = "nakit";
            public string kalem { get; set; } = string.Empty;
            public string aciklama { get; set; } = string.Empty;
        }

        public sealed class KayitKaydetIstek
        {
            public int? id { get; set; }
            public string? tarih { get; set; }
            public string? tur { get; set; }
            public decimal tutar { get; set; }
            public string? odemeYontemi { get; set; }
            public string? kalem { get; set; }
            public string? aciklama { get; set; }
            public StokGirisIstek? stokGiris { get; set; }
        }

        public sealed class StokGirisIstek
        {
            public bool aktif { get; set; }
            public int urunId { get; set; }
            public decimal miktar { get; set; }
        }
    }
}
