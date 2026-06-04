using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Systemcel.Api.Printing;
using Systemcel.Api.Services;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Systemcel.Api.Api
{
    internal sealed class ScreenApi
    {
        private readonly IKasaService? _kasaService;
        private readonly ISummaryService? _summaryService;
        private readonly IIsletmeService? _isletmeService;
        private readonly IKalemTanimiService? _kalemTanimiService;
        private readonly IUrunHizmetService? _urunHizmetService;
        private readonly IStokService? _stokService;
        private readonly IDashboardSnapshotService? _dashboardSnapshotService;
        private readonly BackupReportService? _backupReportService;
        private readonly TelegramSettings? _telegramSettings;
        private readonly ITelegramPairingService? _telegramPairingService;
        private readonly ICariService? _cariService;
        private readonly IFaturaService? _faturaService;
        private readonly IGibPortalService? _gibPortalService;
        private readonly ITahsilatOdemeService? _tahsilatOdemeService;
        private readonly IOnMuhasebeReportService? _onMuhasebeReportService;
        private readonly AppRuntimeOptions? _runtimeOptions;
        private readonly IAppSecurityService? _appSecurityService;
        private readonly PinReminderService? _pinReminderService;
        private readonly ISystemcelYonetimService? _yonetimService;
        private readonly IMuhasebeciPortalService? _muhasebeciPortalService;
        private readonly IMuhasebeciSohbetMerkeziService? _muhasebeciSohbetMerkeziService;
        private RaporPaketDto? _lastReportPackage;

        public ScreenApi(
            IKasaService kasaService,
            ISummaryService summaryService,
            IIsletmeService isletmeService,
            IKalemTanimiService kalemTanimiService,
            IUrunHizmetService urunHizmetService,
            IStokService stokService,
            IDashboardSnapshotService dashboardSnapshotService,
            BackupReportService backupReportService,
            TelegramSettings telegramSettings,
            ITelegramPairingService telegramPairingService,
            ICariService? cariService = null,
            IFaturaService? faturaService = null,
            IGibPortalService? gibPortalService = null,
            ITahsilatOdemeService? tahsilatOdemeService = null,
            IOnMuhasebeReportService? onMuhasebeReportService = null,
            AppRuntimeOptions? runtimeOptions = null,
            IAppSecurityService? appSecurityService = null,
            PinReminderService? pinReminderService = null,
            ISystemcelYonetimService? yonetimService = null,
            IMuhasebeciPortalService? muhasebeciPortalService = null,
            IMuhasebeciSohbetMerkeziService? muhasebeciSohbetMerkeziService = null)
        {
            _kasaService = kasaService;
            _summaryService = summaryService;
            _isletmeService = isletmeService;
            _kalemTanimiService = kalemTanimiService;
            _urunHizmetService = urunHizmetService;
            _stokService = stokService;
            _dashboardSnapshotService = dashboardSnapshotService;
            _backupReportService = backupReportService;
            _telegramSettings = telegramSettings;
            _telegramPairingService = telegramPairingService;
            _cariService = cariService;
            _faturaService = faturaService;
            _gibPortalService = gibPortalService;
            _tahsilatOdemeService = tahsilatOdemeService;
            _onMuhasebeReportService = onMuhasebeReportService;
            _runtimeOptions = runtimeOptions;
            _appSecurityService = appSecurityService;
            _pinReminderService = pinReminderService;
            _yonetimService = yonetimService;
            _muhasebeciPortalService = muhasebeciPortalService;
            _muhasebeciSohbetMerkeziService = muhasebeciSohbetMerkeziService;
        }

        public void MapApi(WebApplication app)
        {
            if (_isletmeService is not null)
            {
                app.MapGet("/api/ekran/ust-bar", async () =>
                {
                    try
                    {
                        return Results.Ok(await BuildTopBarAsync());
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Üst bar verileri yüklenemedi: {ex.Message}");
                    }
                });

                app.MapPut("/api/ekran/ust-bar/isletme/{id:int}", async (int id) =>
                {
                    try
                    {
                        await _isletmeService!.SetActiveAsync(id);
                        return Results.Ok(await BuildTopBarAsync());
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"İşletme değiştirilemedi: {ex.Message}"));
                    }
                });

                app.MapGet("/api/ekran/kolay-kurulum", async () =>
                {
                    try
                    {
                        return Results.Ok(await BuildEasySetupAsync());
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Kolay kurulum yÃ¼klenemedi: {ex.Message}");
                    }
                });
            }

            if (_isletmeService is not null)
            {
                app.MapGet("/api/ekran/bildirimler", async () =>
                {
                    try
                    {
                        return Results.Ok(await BuildNotificationsAsync());
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Bildirimler yÃ¼klenemedi: {ex.Message}");
                    }
                });
            }

            if (_isletmeService is not null && _kalemTanimiService is not null)
            {
                app.MapPost("/api/ekran/kolay-kurulum", async (KolayKurulumKaydetIstek request) =>
                {
                    try
                    {
                        var active = await _isletmeService!.GetActiveAsync();
                        var preset = ResolveEasySetupPreset(request?.isletmeTuru);
                        await _isletmeService.UpdateSetupAsync(
                            active.Id,
                            request?.isletmeAdi ?? string.Empty,
                            preset.kod,
                            request?.konum ?? string.Empty,
                            tamamlandi: true,
                            request?.hesapTipi,
                            request?.muhasebeciVarMi,
                            request?.muhasebeciProfil);

                        foreach (var gelir in preset.gelirKalemleri)
                            await _kalemTanimiService!.CreateAsync("Gelir", gelir);
                        foreach (var gider in preset.giderKalemleri)
                            await _kalemTanimiService!.CreateAsync("Gider", gider);

                        return Results.Ok(await BuildEasySetupAsync("Kurulum tamamlandÄ±."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Kolay kurulum kaydedilemedi: {ex.Message}"));
                    }
                });

                app.MapGet("/api/ekran/ayarlar", async () =>
                {
                    try
                    {
                        return Results.Ok(await BuildSettingsScreenAsync());
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Ayarlar yüklenemedi: {ex.Message}");
                    }
                });

                app.MapPut("/api/ekran/ayarlar/dil", async (AyarDilKaydetIstek request) =>
                {
                    try
                    {
                        UseTurkishLanguage();
                        return Results.Ok(await BuildSettingsScreenAsync("Dil ayarı Türkçe olarak kaydedildi. Çoklu dil desteği web geçişiyle birlikte açılacak."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Dil ayarı kaydedilemedi: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/ayarlar/isletmeler", async (AyarIsletmeKaydetIstek request) =>
                {
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        var id = await _isletmeService!.CreateAsync(request?.ad ?? string.Empty, makeActive: true);
                        return Results.Ok(await BuildSettingsScreenAsync("Yeni işletme eklendi.", id));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"İşletme eklenemedi: {ex.Message}"));
                    }
                });

                app.MapPut("/api/ekran/ayarlar/isletmeler/{id:int}", async (int id, AyarIsletmeKaydetIstek request) =>
                {
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _isletmeService!.RenameAsync(id, request?.ad ?? string.Empty);
                        return Results.Ok(await BuildSettingsScreenAsync("İşletme adı güncellendi.", id));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"İşletme güncellenemedi: {ex.Message}"));
                    }
                });

                app.MapPut("/api/ekran/ayarlar/isletmeler/{id:int}/aktif", async (int id) =>
                {
                    try
                    {
                        await _isletmeService!.SetActiveAsync(id);
                        return Results.Ok(await BuildSettingsScreenAsync("Aktif işletme değiştirildi.", id));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Aktif işletme değiştirilemedi: {ex.Message}"));
                    }
                });

                app.MapDelete("/api/ekran/ayarlar/isletmeler/{id:int}", async (int id) =>
                {
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _isletmeService!.DeleteAsync(id);
                        return Results.Ok(await BuildSettingsScreenAsync("İşletme silindi."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"İşletme silinemedi: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/ayarlar/kalemler", async (AyarKalemKaydetIstek request) =>
                {
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        var id = await _kalemTanimiService!.CreateAsync(request?.tip ?? string.Empty, request?.ad ?? string.Empty);
                        return Results.Ok(await BuildSettingsScreenAsync("Kalem eklendi.", preferredKalemId: id));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Kalem eklenemedi: {ex.Message}"));
                    }
                });

                app.MapPut("/api/ekran/ayarlar/kalemler/{id:int}", async (int id, AyarKalemKaydetIstek request) =>
                {
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _kalemTanimiService!.UpdateAsync(id, request?.ad ?? string.Empty);
                        return Results.Ok(await BuildSettingsScreenAsync("Kalem güncellendi.", preferredKalemId: id));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Kalem güncellenemedi: {ex.Message}"));
                    }
                });

                app.MapDelete("/api/ekran/ayarlar/kalemler/{id:int}", async (int id) =>
                {
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _kalemTanimiService!.DeleteAsync(id);
                        return Results.Ok(await BuildSettingsScreenAsync("Kalem silindi."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Kalem silinemedi: {ex.Message}"));
                    }
                });
            }

            if (_runtimeOptions is not null && _telegramSettings is not null)
            {
                app.MapGet("/api/ekran/telegram", () =>
                {
                    try
                    {
                        return Results.Ok(BuildTelegramScreen());
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Telegram bağlantısı yüklenemedi: {ex.Message}");
                    }
                });

                app.MapPost("/api/ekran/telegram/baslat", () =>
                {
                    try
                    {
                        _telegramPairingService?.RenewCode();
                        return Results.Ok(BuildTelegramScreen("Telegram bağlantı linki hazırlandı."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Telegram bağlantısı başlatılamadı: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/telegram/kontrol", () =>
                {
                    try
                    {
                        var message = _telegramSettings.IsEnabled
                            ? "Telegram bağlantısı aktif."
                            : "Bağlantı bekleniyor. Telegram'da SystemcelBot'a /start kodunu gönderdikten sonra resmi bot servisi bağlantıyı doğrulayacak.";
                        return Results.Ok(BuildTelegramScreen(message));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Telegram bağlantısı kontrol edilemedi: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/telegram/test", async () =>
                {
                    if (_backupReportService is null || !_telegramSettings!.IsEnabled)
                        return Results.BadRequest(new ApiHata("Telegram bağlantısı aktif değil."));

                    try
                    {
                        await _backupReportService.SendTextAsync("Systemcel test mesajı: Telegram bağlantınız çalışıyor.");
                        return Results.Ok(BuildTelegramScreen("Test mesajı Telegram'a gönderildi."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Test mesajı gönderilemedi: {ex.Message}"));
                    }
                });

                app.MapDelete("/api/ekran/telegram", () =>
                {
                    try
                    {
                        _telegramSettings.ChatId = string.Empty;
                        _telegramSettings.AllowedUserIds = string.Empty;
                        UserTelegramSetupStore.Save(_runtimeOptions!.AppDataPath, new UserTelegramSetup());
                        _telegramPairingService?.ClearPairing();
                        return Results.Ok(BuildTelegramScreen("Telegram bağlantısı kaldırıldı.", false));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Telegram bağlantısı kaldırılamadı: {ex.Message}"));
                    }
                });
            }

            if (_gibPortalService is not null &&
                _isletmeService is not null)
            {
                app.MapGet("/api/ekran/gib-portal", async () =>
                {
                    try
                    {
                        return Results.Ok(await BuildGibPortalScreenAsync());
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"GİB Portal ayarları yüklenemedi: {ex.Message}");
                    }
                });

                app.MapPost("/api/ekran/gib-portal", async (GibPortalAyarKaydetIstek request) =>
                {
                    var validation = ValidateGibSettingsRequest(request, requirePassword: false);
                    if (validation is not null)
                        return Results.BadRequest(validation);
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _gibPortalService!.SaveSettingsAsync(new GibPortalSaveSettingsRequest
                        {
                            KullaniciKodu = request.kullaniciKodu?.Trim() ?? string.Empty,
                            Sifre = string.IsNullOrWhiteSpace(request.sifre) ? null : request.sifre,
                            TestModu = request.testModu
                        });

                        var screen = await BuildGibPortalScreenAsync();
                        screen.mesaj = "GİB Portal ayarları kaydedildi.";
                        return Results.Ok(screen);
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"GİB Portal ayarları kaydedilemedi: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/gib-portal/test", async (GibPortalAyarKaydetIstek request) =>
                {
                    var validation = ValidateGibSettingsRequest(request, requirePassword: false);
                    if (validation is not null)
                        return Results.BadRequest(validation);
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _gibPortalService!.SaveSettingsAsync(new GibPortalSaveSettingsRequest
                        {
                            KullaniciKodu = request.kullaniciKodu?.Trim() ?? string.Empty,
                            Sifre = string.IsNullOrWhiteSpace(request.sifre) ? null : request.sifre,
                            TestModu = request.testModu
                        });
                        var result = await _gibPortalService.TestConnectionAsync();
                        return Results.Ok(new GibPortalTestSonucDto(result.Success, result.Message));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"GİB Portal bağlantı testi tamamlanamadı: {ex.Message}"));
                    }
                });
            }

            if (_dashboardSnapshotService is not null &&
                _summaryService is not null &&
                _kasaService is not null &&
                _backupReportService is not null &&
                _isletmeService is not null)
            {
                app.MapGet("/api/ekran/anasayfa", async () =>
                {
                    try
                    {
                        return Results.Ok(await BuildDashboardAsync());
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Anasayfa verileri yüklenemedi: {ex.Message}");
                    }
                });

                app.MapPost("/api/ekran/anasayfa/paylas/telegram", async () =>
                {
                    try
                    {
                        return Results.Ok(await SendDashboardSummaryToTelegramAsync());
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Telegram gönderimi başarısız: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/anasayfa/paylas/pdf", async () =>
                {
                    try
                    {
                        return Results.Ok(await SaveDashboardSummaryPdfAsync());
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"PDF oluşturulamadı: {ex.Message}"));
                    }
                });
            }

            if (_onMuhasebeReportService is not null &&
                _summaryService is not null &&
                _kasaService is not null &&
                _isletmeService is not null)
            {
                app.MapGet("/api/ekran/raporlar", async () =>
                {
                    try
                    {
                        return Results.Ok(await BuildReportsScreenAsync());
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Raporlar yüklenemedi: {ex.Message}");
                    }
                });

                app.MapPost("/api/ekran/raporlar/paket", async (RaporPaketOlusturIstek request) =>
                {
                    try
                    {
                        return Results.Ok(await CreateReportPackageAsync(request));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Rapor paketi oluşturulamadı: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/raporlar/klasor-sec", async (RaporYolIstek request) =>
                {
                    try
                    {
                        return Results.Ok(new RaporKlasorDto(await SelectReportFolderAsync(request.yol)));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Klasör seçilemedi: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/raporlar/paket/ac", (RaporYolIstek request) =>
                {
                    try
                    {
                        OpenReportPath(request.yol, selectInFolder: false);
                        return Results.Ok(new ApiMesaj("Rapor paketi açılıyor."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Rapor paketi açılamadı: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/raporlar/paket/klasor", (RaporYolIstek request) =>
                {
                    try
                    {
                        OpenReportPath(request.yol, selectInFolder: true);
                        return Results.Ok(new ApiMesaj("Rapor klasörü açılıyor."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Rapor klasörü açılamadı: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/raporlar/yazdir/pdf", async (RaporYazdirIstek request) =>
                {
                    try
                    {
                        return Results.Ok(await SaveReportPdfAsync(request));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"PDF oluşturulamadı: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/raporlar/yazdir/html", async (RaporYazdirIstek request) =>
                {
                    try
                    {
                        return Results.Ok(await ExportReportHtmlAsync(request));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"HTML dış aktarım yapılamadı: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/raporlar/yazdir", async (RaporYazdirIstek request) =>
                {
                    try
                    {
                        return Results.Ok(await PrintReportAsync(request));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Rapor yazdirilamadi: {ex.Message}"));
                    }
                });
            }

            if (_kasaService is not null &&
                _summaryService is not null &&
                _isletmeService is not null &&
                _kalemTanimiService is not null &&
                _urunHizmetService is not null &&
                _stokService is not null)
            {
                app.MapGet("/api/ekran/gelir-gider", async () =>
                {
                    try
                    {
                        return Results.Ok(await BuildCashflowAsync());
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Gelir/Gider verileri yüklenemedi: {ex.Message}");
                    }
                });

                app.MapPost("/api/ekran/gelir-gider/kayitlar", async (KayitKaydetIstek request) =>
                {
                    var validation = ValidateRequest(request, isUpdate: false);
                    if (validation is not null)
                        return Results.BadRequest(validation);
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        var domain = ToDomainRecord(request);
                        var kayitId = await _kasaService!.CreateAsync(domain);
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
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _kasaService!.UpdateAsync(ToDomainRecord(request));
                        return Results.Ok(new ApiMesaj("Kayıt güncellendi."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Kayıt güncellenemedi: {ex.Message}"));
                    }
                });

                app.MapDelete("/api/ekran/gelir-gider/kayitlar/{id:int}", async (int id) =>
                {
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _kasaService!.DeleteAsync(id);
                        return Results.Ok(new ApiMesaj("Kayıt silindi."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Kayıt silinemedi: {ex.Message}"));
                    }
                });
            }

            if (_cariService is not null && _isletmeService is not null)
            {
                app.MapGet("/api/ekran/cari-hesaplar", async () =>
                {
                    try
                    {
                        return Results.Ok(await BuildCariScreenAsync());
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Cari hesaplar yüklenemedi: {ex.Message}");
                    }
                });

                app.MapGet("/api/ekran/cari-hesaplar/{id:int}", async (int id) =>
                {
                    try
                    {
                        var detay = await BuildCariDetailAsync(id);
                        return detay is null
                            ? Results.NotFound(new ApiHata("Cari kart bulunamadı."))
                            : Results.Ok(detay);
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Cari kart yüklenemedi: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/cari-hesaplar", async (CariKaydetIstek request) =>
                {
                    var validation = ValidateCariRequest(request);
                    if (validation is not null)
                        return Results.BadRequest(validation);
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        var id = await _cariService!.CreateAsync(ToDomainCari(request));
                        return Results.Ok(new KimlikliApiMesaj("Cari kart kaydedildi.", id));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Cari kart kaydedilemedi: {ex.Message}"));
                    }
                });

                app.MapPut("/api/ekran/cari-hesaplar/{id:int}", async (int id, CariKaydetIstek request) =>
                {
                    request.id = id;
                    var validation = ValidateCariRequest(request);
                    if (validation is not null)
                        return Results.BadRequest(validation);
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _cariService!.UpdateAsync(ToDomainCari(request));
                        return Results.Ok(new KimlikliApiMesaj("Cari kart güncellendi.", id));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Cari kart güncellenemedi: {ex.Message}"));
                    }
                });

                app.MapDelete("/api/ekran/cari-hesaplar/{id:int}", async (int id) =>
                {
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _cariService!.DeleteAsync(id);
                        return Results.Ok(new ApiMesaj("Cari kart silindi."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Cari kart silinemedi: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/cari-hesaplar/{id:int}/hareketler", async (int id, CariHareketKaydetIstek request) =>
                {
                    var validation = ValidateCariMovementRequest(request);
                    if (validation is not null)
                        return Results.BadRequest(validation);
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _cariService!.CreateHareketAsync(ToDomainCariMovement(id, request));
                        return Results.Ok(new ApiMesaj("Cari hareket eklendi."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Cari hareket eklenemedi: {ex.Message}"));
                    }
                });
            }

            if (_urunHizmetService is not null && _stokService is not null && _isletmeService is not null)
            {
                app.MapGet("/api/ekran/urun-stok", async () =>
                {
                    try
                    {
                        return Results.Ok(await BuildProductStockScreenAsync());
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Ürün/Stok verileri yüklenemedi: {ex.Message}");
                    }
                });

                app.MapGet("/api/ekran/urun-stok/barkod", async (string deger) =>
                {
                    try
                    {
                        var row = await _urunHizmetService!.GetByBarcodeAsync(deger);
                        return row is null
                            ? Results.NotFound(new ApiHata("Bu barkodla kayıtlı ürün bulunamadı."))
                            : Results.Ok(await ToProductRowDtoAsync(row));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Barkod kontrol edilemedi: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/urun-stok/urunler", async (UrunHizmetKaydetIstek request) =>
                {
                    var validation = ValidateProductRequest(request);
                    if (validation is not null)
                        return Results.BadRequest(validation);
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        var id = await _urunHizmetService!.CreateAsync(ToProductCreateRequest(request));
                        return Results.Ok(new KimlikliApiMesaj("Ürün/hizmet kaydedildi.", id));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Ürün/hizmet kaydedilemedi: {ex.Message}"));
                    }
                });

                app.MapPut("/api/ekran/urun-stok/urunler/{id:int}", async (int id, UrunHizmetKaydetIstek request) =>
                {
                    request.id = id;
                    var validation = ValidateProductRequest(request);
                    if (validation is not null)
                        return Results.BadRequest(validation);
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _urunHizmetService!.UpdateAsync(ToDomainProduct(request));
                        return Results.Ok(new KimlikliApiMesaj("Ürün/hizmet güncellendi.", id));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Ürün/hizmet güncellenemedi: {ex.Message}"));
                    }
                });

                app.MapDelete("/api/ekran/urun-stok/urunler/{id:int}", async (int id) =>
                {
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _urunHizmetService!.DeleteAsync(id);
                        return Results.Ok(new ApiMesaj("Ürün/hizmet silindi."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Ürün/hizmet silinemedi: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/urun-stok/urunler/{id:int}/hareketler", async (int id, StokHareketKaydetIstek request) =>
                {
                    var validation = ValidateStockMovementRequest(request);
                    if (validation is not null)
                        return Results.BadRequest(validation);
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        var result = await _stokService!.CreateMovementAsync(new StokHareketCreateRequest
                        {
                            UrunHizmetId = id,
                            Tarih = ParseDate(request.tarih),
                            Miktar = request.miktar,
                            Kaynak = "Manuel",
                            Aciklama = request.aciklama
                        });

                        return Results.Ok(new StokHareketSonucDto
                        {
                            mesaj = result.MevcutStok < 0
                                ? $"Stok işlendi. Uyarı: mevcut stok negatif ({result.MevcutStok:N2})."
                                : $"Stok işlendi. Mevcut stok: {result.MevcutStok:N2}",
                            mevcutStok = result.MevcutStok
                        });
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Stok hareketi eklenemedi: {ex.Message}"));
                    }
                });
            }

            if (_faturaService is not null &&
                _cariService is not null &&
                _urunHizmetService is not null &&
                _isletmeService is not null)
            {
                app.MapGet("/api/ekran/faturalar", async () =>
                {
                    try
                    {
                        return Results.Ok(await BuildInvoiceScreenAsync());
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Faturalar yüklenemedi: {ex.Message}");
                    }
                });

                app.MapGet("/api/ekran/faturalar/{id:int}", async (int id) =>
                {
                    try
                    {
                        var detail = await BuildInvoiceDetailAsync(id);
                        return detail is null
                            ? Results.NotFound(new ApiHata("Fatura bulunamadı."))
                            : Results.Ok(detail);
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Fatura yüklenemedi: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/faturalar", async (FaturaKaydetIstek request) =>
                {
                    var validation = ValidateInvoiceRequest(request);
                    if (validation is not null)
                        return Results.BadRequest(validation);
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        var id = await _faturaService!.CreateDraftAsync(ToInvoiceCreateRequest(request));
                        return Results.Ok(new KimlikliApiMesaj("Fatura taslağı oluşturuldu.", id));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Fatura taslağı oluşturulamadı: {ex.Message}"));
                    }
                });

                app.MapPut("/api/ekran/faturalar/{id:int}", async (int id, FaturaKaydetIstek request) =>
                {
                    request.id = id;
                    var validation = ValidateInvoiceRequest(request);
                    if (validation is not null)
                        return Results.BadRequest(validation);
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _faturaService!.UpdateDraftAsync(id, ToInvoiceCreateRequest(request));
                        return Results.Ok(new KimlikliApiMesaj("Fatura taslağı güncellendi.", id));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Fatura taslağı güncellenemedi: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/faturalar/{id:int}/gib-taslak", async (int id) =>
                {
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    if (_gibPortalService is null)
                        return Results.BadRequest(new ApiHata("GİB portal servisi hazır değil."));

                    try
                    {
                        var result = await _gibPortalService.CreatePortalDraftAsync(id);
                        return result.Success
                            ? Results.Ok(new ApiMesaj(result.Message))
                            : Results.BadRequest(new ApiHata(result.Message));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"GİB taslak oluşturulamadı: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/faturalar/{id:int}/gib-sms-baslat", async (int id) =>
                {
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    if (_gibPortalService is null)
                        return Results.BadRequest(new ApiHata("GİB portal servisi hazır değil."));

                    try
                    {
                        var result = await _gibPortalService.StartSmsApprovalAsync(id);
                        return result.Success
                            ? Results.Ok(new GibSmsBaslatDto(result.Message, result.OperationId))
                            : Results.BadRequest(new ApiHata(result.Message));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"GİB SMS onayı başlatılamadı: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/faturalar/{id:int}/gib-sms-tamamla", async (int id, GibSmsTamamlaIstek request) =>
                {
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    if (_gibPortalService is null)
                        return Results.BadRequest(new ApiHata("GİB portal servisi hazır değil."));

                    if (string.IsNullOrWhiteSpace(request.operationId) || string.IsNullOrWhiteSpace(request.smsKodu))
                        return Results.BadRequest(new ApiHata("SMS kodu ve işlem bilgisi gerekli."));

                    try
                    {
                        var result = await _gibPortalService.CompleteSmsApprovalAsync(id, request.operationId.Trim(), request.smsKodu.Trim());
                        return result.Success
                            ? Results.Ok(new ApiMesaj(result.Message))
                            : Results.BadRequest(new ApiHata(result.Message));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"GİB SMS onayı tamamlanamadı: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/faturalar/{id:int}/kes", async (int id) =>
                {
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _faturaService!.MarkAsIssuedAsync(id);
                        return Results.Ok(new ApiMesaj("Fatura kesildi."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Fatura kesilemedi: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/faturalar/{id:int}/iptal", async (int id) =>
                {
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        await _faturaService!.CancelAsync(id);
                        return Results.Ok(new ApiMesaj("Fatura iptal edildi."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Fatura iptal edilemedi: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/faturalar/{id:int}/tahsilat-odeme", async (int id, FaturaTahsilatKaydetIstek request) =>
                {
                    var validation = ValidateInvoicePaymentRequest(request);
                    if (validation is not null)
                        return Results.BadRequest(validation);
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    if (_tahsilatOdemeService is null)
                        return Results.BadRequest(new ApiHata("Tahsilat/ödeme servisi hazır değil."));

                    try
                    {
                        await _tahsilatOdemeService.CreateAsync(new TahsilatOdemeRequest
                        {
                            FaturaId = id,
                            Tarih = ParseDate(request.tarih),
                            Tutar = request.tutar,
                            OdemeYontemi = ToDomainPayment(request.odemeYontemi),
                            Aciklama = request.aciklama
                        });
                        return Results.Ok(new ApiMesaj("Tahsilat/ödeme eklendi."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Tahsilat/ödeme eklenemedi: {ex.Message}"));
                    }
                });
            }

            if (_cariService is not null &&
                _isletmeService is not null)
            {
                app.MapGet("/api/ekran/tahsilat-odeme", async () =>
                {
                    try
                    {
                        return Results.Ok(await BuildPaymentScreenAsync());
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Tahsilat/ödeme verileri yüklenemedi: {ex.Message}");
                    }
                });

                app.MapPost("/api/ekran/tahsilat-odeme", async (TahsilatOdemeKaydetIstek request) =>
                {
                    var validation = ValidatePaymentScreenRequest(request);
                    if (validation is not null)
                        return Results.BadRequest(validation);
                    var readOnly = await RejectReadOnlyAccountantContextAsync();
                    if (readOnly is not null)
                        return readOnly;

                    try
                    {
                        if (request.faturaIleEslestir)
                        {
                            if (_tahsilatOdemeService is null)
                                return Results.BadRequest(new ApiHata("Tahsilat/ödeme servisi hazır değil."));

                            await _tahsilatOdemeService.CreateAsync(new TahsilatOdemeRequest
                            {
                                FaturaId = request.faturaId,
                                Tarih = ParseDate(request.tarih),
                                Tutar = request.tutar,
                                OdemeYontemi = ToDomainPayment(request.odemeYontemi),
                                Aciklama = BuildPaymentNote(request)
                            });
                        }
                        else
                        {
                            await _cariService!.CreateHareketAsync(new CariHareket
                            {
                                CariKartId = request.cariKartId,
                                Tarih = ParseDate(request.tarih),
                                HareketTipi = NormalizeCariMovementType(request.islemTipi),
                                Tutar = request.tutar,
                                Kaynak = "Manuel",
                                Aciklama = BuildPaymentNote(request)
                            });
                        }

                        return Results.Ok(new ApiMesaj("Tahsilat/ödeme eklendi."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"Tahsilat/ödeme eklenemedi: {ex.Message}"));
                    }
                });
            }

            if (_appSecurityService is not null)
            {
                app.MapPost("/api/ekran/kilit-ekrani/dogrula", async (PinDogrulaIstek request) =>
                {
                    if (request is null || string.IsNullOrWhiteSpace(request.pin))
                        return Results.BadRequest(new ApiHata("4 haneli PIN girin."));

                    try
                    {
                        var isValid = await _appSecurityService!.VerifyPinAsync(request.pin.Trim());
                        return isValid
                            ? Results.Ok(new ApiMesaj("Giriş başarılı."))
                            : Results.BadRequest(new ApiHata("PIN hatalı. Tekrar deneyin."));
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new ApiHata($"PIN doğrulanamadı: {ex.Message}"));
                    }
                });

                app.MapPost("/api/ekran/kilit-ekrani/hatirlat", async () =>
                {
                    if (_pinReminderService is null)
                        return Results.BadRequest(new ApiHata("PIN hatırlatma servisi hazır değil."));

                    var result = await _pinReminderService.SendCurrentPinAsync();
                    return result.Status == PinReminderStatus.Success
                        ? Results.Ok(new ApiMesaj(result.Message))
                        : Results.BadRequest(new ApiHata(result.Message));
                });
            }
        }

        private async Task<DashboardEkranDto> BuildDashboardAsync()
        {
            var today = DateTime.Today;
            var snapshot = await _dashboardSnapshotService!.GetSnapshotAsync(
                today,
                SummaryRangeCatalog.Last30Days,
                SummaryRangeCatalog.Last1Year,
                today.Month,
                today.Year,
                today.Year);

            var yesterday = today.AddDays(-1);
            var yesterdaySummary = await _summaryService!.GetSummaryAsync(yesterday, yesterday);
            var recentRows = await _kasaService!.GetAllAsync(today.AddDays(-29), today);
            var chatStatus = await BuildChatNotificationStatusAsync();

            return new DashboardEkranDto
            {
                aktifIsletme = string.IsNullOrWhiteSpace(snapshot.ActiveBusinessName) ? "Bilinmiyor" : snapshot.ActiveBusinessName.Trim(),
                bugun = ToOzetDto("Bugun", snapshot.DailySummary),
                paneller = new List<OzetKartDto>
                {
                    ToOzetDto("Son 30 Gun", snapshot.PrimaryRangeSummary),
                    ToOzetDto("Bu Ay", snapshot.MonthlySummary),
                    ToOzetDto("Bu Yil", snapshot.YearlySummary),
                    ToOzetDto("Son 1 Yil", snapshot.SecondaryRangeSummary)
                },
                gelirDegisim = BuildDeltaDto(snapshot.DailySummary.IncomeTotal, yesterdaySummary.IncomeTotal, positiveIsGood: true),
                giderDegisim = BuildDeltaDto(snapshot.DailySummary.ExpenseTotal, yesterdaySummary.ExpenseTotal, positiveIsGood: false),
                odemeDagilimi = snapshot.DailyPaymentMethodBreakdowns
                    .Select(ToOdemeDagilimDto)
                    .ToList(),
                netTrend = BuildNetTrend(recentRows, today, 30),
                sohbet = chatStatus
            };
        }

        private async Task<UstBarDto> BuildTopBarAsync()
        {
            var activeBusiness = await _isletmeService!.GetActiveAsync();
            var access = await _isletmeService.GetActiveAccessAsync();
            var businesses = await _isletmeService!.GetAllAsync();
            var notifications = await BuildNotificationsSafelyAsync();
            var chatStatus = await BuildChatNotificationStatusAsync();
            var accountant = access.MuhasebeciIsletmeId.HasValue
                ? await _isletmeService.GetByIdAsync(access.MuhasebeciIsletmeId.Value)
                : null;

            return new UstBarDto
            {
                aktifIsletmeId = activeBusiness.Id,
                aktifIsletme = string.IsNullOrWhiteSpace(activeBusiness.Ad) ? "Bilinmiyor" : activeBusiness.Ad.Trim(),
                hesapTipi = string.IsNullOrWhiteSpace(activeBusiness.TenantTipi) ? HesapTipleri.Isletme : activeBusiness.TenantTipi,
                muhasebeciMusteriBaglami = access.MuhasebeciMusteriBaglami,
                muhasebeciIsletmeId = access.MuhasebeciIsletmeId,
                muhasebeciAdi = string.IsNullOrWhiteSpace(accountant?.Ad) ? string.Empty : accountant!.Ad.Trim(),
                muhasebeciYetkiSeviyesi = access.YetkiSeviyesi,
                telegramAktif = _telegramSettings?.IsEnabled ?? false,
                bildirimVar = notifications.Count > 0,
                bildirimSayisi = notifications.Count,
                sohbet = chatStatus,
                yoneticiMi = _yonetimService != null && await _yonetimService.IsCurrentUserAdminAsync(),
                isletmeler = businesses
                    .Select(x => new IsletmeSecenekDto
                    {
                        id = x.Id,
                        ad = string.IsNullOrWhiteSpace(x.Ad) ? "İşletme" : x.Ad.Trim(),
                        aktif = x.IsAktif
                    })
                    .ToList()
            };
        }

        private async Task<List<BildirimDto>> BuildNotificationsSafelyAsync()
        {
            try
            {
                return await BuildNotificationsAsync();
            }
            catch
            {
                return new List<BildirimDto>();
            }
        }

        private async Task<IResult?> RejectReadOnlyAccountantContextAsync()
        {
            if (_isletmeService is null)
                return null;

            var access = await _isletmeService.GetActiveAccessAsync();
            return access.MuhasebeciMusteriBaglami && !access.YazmaYetkisi
                ? Results.BadRequest(new ApiHata("Bu mÃ¼ÅŸteri baÄŸlamÄ±nda sadece okuma ve rapor yetkiniz var. KayÄ±t deÄŸiÅŸiklikleri iÃ§in mÃ¼ÅŸterinin tam iÅŸlem yetkisi vermesi gerekir."))
                : null;
        }

        private async Task<MuhasebeciSohbetBildirimDurumuDto> BuildChatNotificationStatusAsync()
        {
            if (_muhasebeciSohbetMerkeziService is not null)
            {
                try
                {
                    var list = await _muhasebeciSohbetMerkeziService.GetSohbetlerAsync();
                    return new MuhasebeciSohbetBildirimDurumuDto
                    {
                        OkunmamisMesajSayisi = list.OkunmamisMesajSayisi,
                        Sohbetler = list.Sohbetler.Select(x => new MuhasebeciSohbetBildirimDto
                        {
                            MuhasebeciIsletmeId = x.MuhasebeciIsletmeId,
                            MusteriIsletmeId = x.MusteriIsletmeId,
                            TalepId = x.TalepId,
                            BaglantiId = x.BaglantiId,
                            Baslik = x.Baslik,
                            SonMesaj = x.SonMesaj,
                            SonMesajAt = x.SonMesajAt ?? DateTime.Now,
                            OkunmamisMesajSayisi = x.OkunmamisMesajSayisi,
                            HedefUrl = x.HedefUrl
                        }).ToList()
                    };
                }
                catch
                {
                    return new MuhasebeciSohbetBildirimDurumuDto();
                }
            }

            if (_muhasebeciPortalService is null)
                return new MuhasebeciSohbetBildirimDurumuDto();

            try
            {
                return await _muhasebeciPortalService.GetConversationNotificationStatusAsync();
            }
            catch
            {
                return new MuhasebeciSohbetBildirimDurumuDto();
            }
        }

        private async Task<List<BildirimDto>> BuildNotificationsAsync()
        {
            var notifications = new List<BildirimDto>();

            if (_faturaService is null || _cariService is null)
                return notifications;

            var today = DateTime.Today;
            var invoices = await _faturaService.GetAllAsync();
            var openInvoices = invoices
                .Where(x => x.Durum is not FaturaDurum.Odendi and not FaturaDurum.Iptal)
                .Select(x => new
                {
                    Fatura = x,
                    Kalan = Math.Max(0, x.GenelToplam - x.OdenenTutar)
                })
                .Where(x => x.Kalan > 0 && x.Fatura.VadeTarihi.HasValue)
                .ToList();

            var cariIds = openInvoices.Select(x => x.Fatura.CariKartId).Distinct().ToList();
            var cariMap = new Dictionary<int, string>();
            foreach (var cariId in cariIds)
            {
                var cari = await _cariService.GetByIdAsync(cariId);
                cariMap[cariId] = string.IsNullOrWhiteSpace(cari?.Unvan) ? "Cari kurum" : cari!.Unvan.Trim();
            }

            var upcomingEnd = today.AddDays(7);

            var upcomingPayment = openInvoices
                .Where(x => IsInvoiceType(x.Fatura.FaturaTipi, "Alis") && x.Fatura.VadeTarihi!.Value.Date >= today && x.Fatura.VadeTarihi!.Value.Date <= upcomingEnd)
                .OrderBy(x => x.Fatura.VadeTarihi)
                .ThenByDescending(x => x.Kalan)
                .FirstOrDefault();
            if (upcomingPayment is not null)
            {
                var name = GetCariName(cariMap, upcomingPayment.Fatura.CariKartId);
                notifications.Add(new BildirimDto
                {
                    id = $"odeme-yaklasiyor-{upcomingPayment.Fatura.Id}",
                    tur = "odeme",
                    onem = upcomingPayment.Fatura.VadeTarihi!.Value.Date == today ? "yuksek" : "orta",
                    baslik = $"{name} ödemen yaklaşıyor",
                    mesaj = $"{FormatDate(upcomingPayment.Fatura.VadeTarihi)} vadesine {FormatMoney(upcomingPayment.Kalan)} kaldı.",
                    aksiyon = "Ödemeyi planla"
                });
            }

            var upcomingCollection = openInvoices
                .Where(x => IsInvoiceType(x.Fatura.FaturaTipi, "Satis") && x.Fatura.VadeTarihi!.Value.Date >= today && x.Fatura.VadeTarihi!.Value.Date <= upcomingEnd)
                .OrderBy(x => x.Fatura.VadeTarihi)
                .ThenByDescending(x => x.Kalan)
                .FirstOrDefault();
            if (upcomingCollection is not null)
            {
                var name = GetCariName(cariMap, upcomingCollection.Fatura.CariKartId);
                notifications.Add(new BildirimDto
                {
                    id = $"tahsilat-yaklasiyor-{upcomingCollection.Fatura.Id}",
                    tur = "tahsilat",
                    onem = "orta",
                    baslik = $"{name} tahsilatı yaklaşıyor",
                    mesaj = $"{FormatDate(upcomingCollection.Fatura.VadeTarihi)} vadesinde {FormatMoney(upcomingCollection.Kalan)} bekleniyor.",
                    aksiyon = "Tahsilatı takip et"
                });
            }

            var overdueReceivableGroups = openInvoices
                .Where(x => IsInvoiceType(x.Fatura.FaturaTipi, "Satis") && x.Fatura.VadeTarihi!.Value.Date < today)
                .GroupBy(x => x.Fatura.CariKartId)
                .Select(g => new
                {
                    CariKartId = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(x => x.Kalan),
                    OldestDue = g.Min(x => x.Fatura.VadeTarihi!.Value.Date)
                })
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.Total)
                .FirstOrDefault();
            if (overdueReceivableGroups is not null)
            {
                var name = GetCariName(cariMap, overdueReceivableGroups.CariKartId);
                notifications.Add(new BildirimDto
                {
                    id = $"geciken-tahsilat-{overdueReceivableGroups.CariKartId}",
                    tur = "risk",
                    onem = overdueReceivableGroups.Count >= 2 ? "yuksek" : "orta",
                    baslik = overdueReceivableGroups.Count >= 2
                        ? $"{name} sana vereceğini {overdueReceivableGroups.Count} seferdir geç ödüyor"
                        : $"{name} tahsilatı gecikti",
                    mesaj = $"{FormatMoney(overdueReceivableGroups.Total)} açık alacak var. En eski vade {FormatDate(overdueReceivableGroups.OldestDue)}.",
                    aksiyon = "Tahsilatı öne al"
                });
            }

            var overduePayment = openInvoices
                .Where(x => IsInvoiceType(x.Fatura.FaturaTipi, "Alis") && x.Fatura.VadeTarihi!.Value.Date < today)
                .OrderBy(x => x.Fatura.VadeTarihi)
                .ThenByDescending(x => x.Kalan)
                .FirstOrDefault();
            if (overduePayment is not null)
            {
                var name = GetCariName(cariMap, overduePayment.Fatura.CariKartId);
                notifications.Add(new BildirimDto
                {
                    id = $"geciken-odeme-{overduePayment.Fatura.Id}",
                    tur = "odeme",
                    onem = "yuksek",
                    baslik = $"{name} ödemen gecikti",
                    mesaj = $"{FormatMoney(overduePayment.Kalan)} ödeme {FormatDate(overduePayment.Fatura.VadeTarihi)} vadesinden beri açık.",
                    aksiyon = "Ödemeyi kapat"
                });
            }

            var nextPaymentsTotal = openInvoices
                .Where(x => IsInvoiceType(x.Fatura.FaturaTipi, "Alis") && x.Fatura.VadeTarihi!.Value.Date >= today && x.Fatura.VadeTarihi!.Value.Date <= upcomingEnd)
                .Sum(x => x.Kalan);
            var nextCollectionsTotal = openInvoices
                .Where(x => IsInvoiceType(x.Fatura.FaturaTipi, "Satis") && x.Fatura.VadeTarihi!.Value.Date >= today && x.Fatura.VadeTarihi!.Value.Date <= upcomingEnd)
                .Sum(x => x.Kalan);
            if (nextPaymentsTotal > 0 && nextPaymentsTotal > nextCollectionsTotal)
            {
                notifications.Add(new BildirimDto
                {
                    id = "haftalik-nakit-dengesi",
                    tur = "nakit",
                    onem = "orta",
                    baslik = "Bu hafta ödeme baskısı var",
                    mesaj = $"Yaklaşan ödeme {FormatMoney(nextPaymentsTotal)}, beklenen tahsilat {FormatMoney(nextCollectionsTotal)}.",
                    aksiyon = "Nakit planını kontrol et"
                });
            }

            return notifications
                .GroupBy(x => x.id)
                .Select(g => g.First())
                .OrderByDescending(x => x.onem == "yuksek")
                .ThenBy(x => x.baslik)
                .Take(5)
                .ToList();
        }

        private static bool IsInvoiceType(string? value, string expected)
        {
            return string.Equals((value ?? string.Empty).Trim(), expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCariName(IReadOnlyDictionary<int, string> cariMap, int cariKartId)
        {
            return cariMap.TryGetValue(cariKartId, out var name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : "Cari kurum";
        }

        private static string FormatMoney(decimal value)
        {
            return value.ToString("C2", CultureInfo.GetCultureInfo("tr-TR"));
        }

        private static string FormatDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) : "-";
        }

        private async Task<KolayKurulumEkranDto> BuildEasySetupAsync(string mesaj = "")
        {
            var activeBusiness = await _isletmeService!.GetActiveAsync();
            return new KolayKurulumEkranDto
            {
                tamamlandi = activeBusiness.KolayKurulumTamamlandi,
                isletmeId = activeBusiness.Id,
                isletmeAdi = string.IsNullOrWhiteSpace(activeBusiness.Ad) ? "Mevcut İşletme" : activeBusiness.Ad.Trim(),
                hesapTipi = string.IsNullOrWhiteSpace(activeBusiness.TenantTipi) ? HesapTipleri.Isletme : activeBusiness.TenantTipi,
                isletmeTuru = string.IsNullOrWhiteSpace(activeBusiness.IsletmeTuru) ? "Genel" : activeBusiness.IsletmeTuru.Trim(),
                konum = activeBusiness.Konum?.Trim() ?? string.Empty,
                muhasebeciVarMi = activeBusiness.MuhasebeciVarMi,
                mesaj = mesaj,
                turler = BuildEasySetupTypes().Concat(BuildExtraEasySetupTypes()).ToList()
            };
        }

        private static KolayKurulumTurDto ResolveEasySetupPreset(string? requestedType)
        {
            var normalized = string.IsNullOrWhiteSpace(requestedType) ? "Genel" : requestedType.Trim();
            var types = BuildEasySetupTypes().Concat(BuildExtraEasySetupTypes()).ToList();
            return types.FirstOrDefault(x => string.Equals(x.kod, normalized, StringComparison.OrdinalIgnoreCase))
                ?? types.First(x => x.kod == "Genel");
        }

        private static List<KolayKurulumTurDto> BuildEasySetupTypes()
        {
            return new List<KolayKurulumTurDto>
            {
                new()
                {
                    kod = "MuhasebeOfisi",
                    ad = "Muhasebe Ofisi",
                    aciklama = "Müşteri portföyü, raporlama ve dönem takibi için.",
                    gelirKalemleri = new() { "Muhasebe Hizmeti", "Danışmanlık Geliri", "Dönem Takip Hizmeti" },
                    giderKalemleri = new() { "Ofis Giderleri", "Yazılım Abonelikleri", "Personel Giderleri", "Ulaşım Giderleri" }
                },
                new()
                {
                    kod = "RestoranKafe",
                    ad = "Restoran / Kafe",
                    aciklama = "Günlük satış, mutfak, personel ve paket servis takibi için.",
                    gelirKalemleri = new() { "Satış Geliri", "Paket Servis", "Catering / Toplu Sipariş" },
                    giderKalemleri = new() { "Mutfak Giderleri", "Gıda ve İçecek Alımları", "Personel Giderleri", "Kira Giderleri", "Kurye / Paket Servis", "Pazarlama Giderleri" }
                },
                new()
                {
                    kod = "PerakendeMagaza",
                    ad = "Perakende / Mağaza",
                    aciklama = "Mağaza satışı, stok alımı, kira ve kargo takibi için.",
                    gelirKalemleri = new() { "Mağaza Satışları", "Online Satış", "Kampanya Geliri" },
                    giderKalemleri = new() { "Stok Alımları", "Kira Giderleri", "Personel Giderleri", "Kargo Giderleri", "Pazarlama Giderleri" }
                },
                new()
                {
                    kod = "ETicaret",
                    ad = "E-ticaret",
                    aciklama = "Pazaryeri, kargo, reklam ve depo maliyetleri için.",
                    gelirKalemleri = new() { "Online Satış", "Pazaryeri Geliri", "Abonelik / Üyelik Geliri" },
                    giderKalemleri = new() { "Ürün Alımı", "Pazaryeri Komisyonu", "Kargo Giderleri", "Reklam Giderleri", "Depolama Giderleri" }
                },
                new()
                {
                    kod = "HizmetDanismanlik",
                    ad = "Hizmet / Danışmanlık",
                    aciklama = "Proje, hizmet, yazılım ve operasyon giderleri için.",
                    gelirKalemleri = new() { "Hizmet Geliri", "Proje Geliri", "Abonelik Geliri" },
                    giderKalemleri = new() { "Personel Giderleri", "Yazılım Abonelikleri", "Ofis Giderleri", "Ulaşım Giderleri", "Pazarlama Giderleri" }
                },
                new()
                {
                    kod = "Uretim",
                    ad = "Üretim / Atölye",
                    aciklama = "Hammadde, işçilik, makine bakımı ve sevkiyat takibi için.",
                    gelirKalemleri = new() { "Ürün Satışı", "Toptan Satış", "Özel Sipariş" },
                    giderKalemleri = new() { "Hammadde Alımı", "Personel Giderleri", "Makine Bakımı", "Enerji Giderleri", "Lojistik Giderleri" }
                },
                new()
                {
                    kod = "Genel",
                    ad = "Genel İşletme",
                    aciklama = "Temel gelir, gider, fatura ve operasyon takibi için.",
                    gelirKalemleri = new() { "Genel Gelir", "Satış Geliri", "Hizmet Geliri" },
                    giderKalemleri = new() { "Genel Gider", "Kira Giderleri", "Fatura Giderleri", "Personel Giderleri", "Pazarlama Giderleri" }
                }
            };
        }

        private static List<KolayKurulumTurDto> BuildExtraEasySetupTypes()
        {
            return new List<KolayKurulumTurDto>
            {
                EasySetupType("PazaryeriSaticisi", "Pazaryeri Satıcısı", "Trendyol, Hepsiburada, Amazon ve benzeri kanal takibi için.", new[] { "Pazaryeri Satışı", "Kampanya Geliri", "İade Dışı Net Satış" }, new[] { "Komisyon Giderleri", "Kargo Giderleri", "Reklam Giderleri", "İade Giderleri", "Depo Giderleri" }),
                EasySetupType("YazilimTeknoloji", "Yazılım / Teknoloji", "SaaS, proje geliştirme, bakım ve lisans gelirleri için.", new[] { "Lisans Geliri", "Abonelik Geliri", "Proje Geliri", "Bakım Geliri" }, new[] { "Bulut Servisleri", "Yazılım Lisansları", "Personel Giderleri", "Donanım Giderleri", "Pazarlama Giderleri" }),
                EasySetupType("GidaUretimi", "Gıda Üretimi", "İmalat, paketleme, soğuk zincir ve dağıtım giderleri için.", new[] { "Ürün Satışı", "Toptan Satış", "Özel Üretim" }, new[] { "Hammadde Alımı", "Ambalaj Giderleri", "Soğuk Zincir", "Personel Giderleri", "Denetim Giderleri" }),
                EasySetupType("ToptanTicaret", "Toptan Ticaret", "Tedarik, bayi satışı, depo ve sevkiyat takibi için.", new[] { "Toptan Satış", "Bayi Satışı", "Sevkiyat Geliri" }, new[] { "Ürün Alımı", "Depo Giderleri", "Nakliye Giderleri", "Personel Giderleri", "Finansman Giderleri" }),
                EasySetupType("SaglikKlinik", "Sağlık / Klinik", "Muayene, işlem, medikal sarf ve personel takibi için.", new[] { "Muayene Geliri", "İşlem Geliri", "Paket Hizmet" }, new[] { "Medikal Sarf", "Personel Giderleri", "Cihaz Bakımı", "Kira Giderleri", "Lisans / Ruhsat Giderleri" }),
                EasySetupType("Eczane", "Eczane", "Reçete, perakende satış, stok ve tedarikçi takibi için.", new[] { "Reçete Geliri", "Perakende Satış", "Kozmetik Satışı" }, new[] { "İlaç Alımı", "Kozmetik Alımı", "Personel Giderleri", "Kira Giderleri", "Fire / İade Giderleri" }),
                EasySetupType("Veteriner", "Veteriner", "Muayene, operasyon, mama ve medikal ürün takibi için.", new[] { "Muayene Geliri", "Operasyon Geliri", "Ürün Satışı" }, new[] { "Medikal Sarf", "Mama / Ürün Alımı", "Personel Giderleri", "Cihaz Bakımı", "Kira Giderleri" }),
                EasySetupType("GuzellikKuafor", "Güzellik / Kuaför", "Randevu, işlem, ürün satışı ve sarf takibi için.", new[] { "İşlem Geliri", "Paket Satışı", "Ürün Satışı" }, new[] { "Sarf Malzeme", "Personel Giderleri", "Kira Giderleri", "Cihaz Bakımı", "Pazarlama Giderleri" }),
                EasySetupType("SporFitness", "Spor / Fitness", "Üyelik, ders, ekipman ve eğitmen giderleri için.", new[] { "Üyelik Geliri", "Özel Ders", "Ürün Satışı" }, new[] { "Ekipman Giderleri", "Eğitmen Giderleri", "Kira Giderleri", "Temizlik Giderleri", "Pazarlama Giderleri" }),
                EasySetupType("EgitimKurs", "Eğitim / Kurs", "Kayıt, ders, materyal ve eğitmen giderleri için.", new[] { "Kurs Geliri", "Ders Geliri", "Materyal Satışı" }, new[] { "Eğitmen Giderleri", "Materyal Giderleri", "Kira Giderleri", "Yazılım Abonelikleri", "Reklam Giderleri" }),
                EasySetupType("InsaatTadilat", "İnşaat / Tadilat", "Hakediş, proje, malzeme ve taşeron takibi için.", new[] { "Hakediş Geliri", "Proje Geliri", "Tadilat Geliri" }, new[] { "Malzeme Alımı", "Taşeron Giderleri", "Personel Giderleri", "Nakliye Giderleri", "Ekipman Kiralama" }),
                EasySetupType("MimarlikMuhendislik", "Mimarlık / Mühendislik", "Proje, çizim, danışmanlık ve saha giderleri için.", new[] { "Proje Geliri", "Danışmanlık Geliri", "Kontrolörlük Geliri" }, new[] { "Yazılım Lisansları", "Saha Giderleri", "Personel Giderleri", "Ofis Giderleri", "Sunum / Baskı Giderleri" }),
                EasySetupType("Emlak", "Emlak / Gayrimenkul", "Komisyon, portföy, ilan ve saha giderleri için.", new[] { "Satış Komisyonu", "Kiralama Komisyonu", "Danışmanlık Geliri" }, new[] { "İlan Giderleri", "Saha Giderleri", "Ofis Giderleri", "Personel Giderleri", "Pazarlama Giderleri" }),
                EasySetupType("OtelKonaklama", "Otel / Konaklama", "Oda, restoran, temizlik ve operasyon takibi için.", new[] { "Oda Geliri", "Restoran Geliri", "Etkinlik Geliri" }, new[] { "Temizlik Giderleri", "Personel Giderleri", "Gıda Alımları", "Enerji Giderleri", "Bakım Giderleri" }),
                EasySetupType("TurizmSeyahat", "Turizm / Seyahat", "Tur, bilet, rehberlik ve operasyon giderleri için.", new[] { "Tur Geliri", "Bilet Komisyonu", "Rehberlik Geliri" }, new[] { "Ulaşım Giderleri", "Rehber Giderleri", "Konaklama Giderleri", "Pazarlama Giderleri", "Sigorta Giderleri" }),
                EasySetupType("LojistikNakliye", "Lojistik / Nakliye", "Taşıma, araç, yakıt ve bakım takibi için.", new[] { "Taşıma Geliri", "Depolama Geliri", "Dağıtım Geliri" }, new[] { "Yakıt Giderleri", "Araç Bakımı", "Şoför Giderleri", "Sigorta Giderleri", "Köprü / Otoyol" }),
                EasySetupType("KargoKurye", "Kargo / Kurye", "Paket dağıtımı, kurye, araç ve rota giderleri için.", new[] { "Teslimat Geliri", "Aylık Hizmet Geliri", "Ekspres Teslimat" }, new[] { "Kurye Giderleri", "Yakıt Giderleri", "Araç Bakımı", "Paketleme Giderleri", "Çağrı Merkezi Giderleri" }),
                EasySetupType("OtomotivServis", "Otomotiv / Servis", "Servis, yedek parça, işçilik ve garanti takibi için.", new[] { "Servis Geliri", "Yedek Parça Satışı", "Ekspertiz Geliri" }, new[] { "Yedek Parça Alımı", "Personel Giderleri", "Ekipman Giderleri", "Kira Giderleri", "Atık Bertaraf" }),
                EasySetupType("Akaryakit", "Akaryakıt / İstasyon", "Yakıt satışı, market, vardiya ve tedarik takibi için.", new[] { "Yakıt Satışı", "Market Satışı", "Yıkama Geliri" }, new[] { "Yakıt Alımı", "Market Ürün Alımı", "Personel Giderleri", "Enerji Giderleri", "Bakım Giderleri" }),
                EasySetupType("AjansMedya", "Ajans / Medya", "Kampanya, prodüksiyon, reklam ve freelancer giderleri için.", new[] { "Proje Geliri", "Retainer Geliri", "Prodüksiyon Geliri" }, new[] { "Freelancer Giderleri", "Reklam Bütçesi", "Yazılım Abonelikleri", "Ekipman Giderleri", "Ofis Giderleri" }),
                EasySetupType("EtkinlikOrganizasyon", "Etkinlik / Organizasyon", "Etkinlik, sahne, ekipman ve tedarikçi takibi için.", new[] { "Organizasyon Geliri", "Bilet Geliri", "Sponsor Geliri" }, new[] { "Mekan Giderleri", "Ekipman Kiralama", "Personel Giderleri", "Tedarikçi Giderleri", "Pazarlama Giderleri" }),
                EasySetupType("TarimHayvancilik", "Tarım / Hayvancılık", "Ürün, yem, gübre, bakım ve hasat takibi için.", new[] { "Ürün Satışı", "Canlı Hayvan Satışı", "Destek Geliri" }, new[] { "Yem Giderleri", "Gübre / İlaç", "Veteriner Giderleri", "Yakıt Giderleri", "İşçilik Giderleri" }),
                EasySetupType("Tekstil", "Tekstil / Konfeksiyon", "Üretim, fason, kumaş ve sevkiyat takibi için.", new[] { "Ürün Satışı", "Fason Üretim", "Toptan Satış" }, new[] { "Kumaş Alımı", "Aksesuar Alımı", "İşçilik Giderleri", "Makine Bakımı", "Sevkiyat Giderleri" }),
                EasySetupType("MobilyaDekorasyon", "Mobilya / Dekorasyon", "Sipariş, üretim, montaj ve malzeme takibi için.", new[] { "Mobilya Satışı", "Özel Sipariş", "Montaj Geliri" }, new[] { "Malzeme Alımı", "Atölye Giderleri", "Montaj Giderleri", "Nakliye Giderleri", "Personel Giderleri" }),
                EasySetupType("Matbaa", "Matbaa / Baskı", "Baskı, tasarım, kağıt ve makine maliyetleri için.", new[] { "Baskı Geliri", "Tasarım Geliri", "Toplu Sipariş" }, new[] { "Kağıt Alımı", "Mürekkep / Sarf", "Makine Bakımı", "Personel Giderleri", "Teslimat Giderleri" }),
                EasySetupType("Kuyumculuk", "Kuyumculuk", "Altın, takı, işçilik ve değerli ürün takibi için.", new[] { "Ürün Satışı", "İşçilik Geliri", "Tamir Geliri" }, new[] { "Ürün Alımı", "İşçilik Giderleri", "Güvenlik Giderleri", "Sigorta Giderleri", "Kira Giderleri" }),
                EasySetupType("HukukDanismanlik", "Hukuk / Danışmanlık", "Dosya, danışmanlık, dava ve ofis giderleri için.", new[] { "Danışmanlık Geliri", "Dosya Geliri", "Sözleşme Geliri" }, new[] { "Ofis Giderleri", "Personel Giderleri", "Harç / Masraf", "Yazılım Abonelikleri", "Ulaşım Giderleri" }),
                EasySetupType("FinansSigorta", "Finans / Sigorta", "Komisyon, danışmanlık ve müşteri portföyü takibi için.", new[] { "Komisyon Geliri", "Danışmanlık Geliri", "Portföy Geliri" }, new[] { "Lisans / Yetki Giderleri", "Personel Giderleri", "Ofis Giderleri", "Pazarlama Giderleri", "Yazılım Abonelikleri" }),
                EasySetupType("TemizlikBakim", "Temizlik / Bakım", "Sözleşmeli hizmet, ekip, sarf ve rota giderleri için.", new[] { "Hizmet Geliri", "Sözleşme Geliri", "Ek İş Geliri" }, new[] { "Sarf Malzeme", "Personel Giderleri", "Ulaşım Giderleri", "Ekipman Giderleri", "Sigorta Giderleri" }),
                EasySetupType("Guvenlik", "Güvenlik Hizmetleri", "Personel, vardiya, ekipman ve sözleşme takibi için.", new[] { "Sözleşme Geliri", "Ek Vardiya Geliri", "Danışmanlık Geliri" }, new[] { "Personel Giderleri", "Ekipman Giderleri", "Eğitim Giderleri", "Ulaşım Giderleri", "Sigorta Giderleri" }),
                EasySetupType("Enerji", "Enerji / Teknik Servis", "Kurulum, bakım, saha ve ekipman giderleri için.", new[] { "Kurulum Geliri", "Bakım Geliri", "Proje Geliri" }, new[] { "Ekipman Alımı", "Saha Giderleri", "Personel Giderleri", "Araç Giderleri", "Sertifika Giderleri" }),
                EasySetupType("DernekVakif", "Dernek / Vakıf", "Bağış, aidat, etkinlik ve proje giderleri için.", new[] { "Bağış Geliri", "Aidat Geliri", "Proje Desteği" }, new[] { "Etkinlik Giderleri", "Personel Giderleri", "Ofis Giderleri", "Yardım Giderleri", "Tanıtım Giderleri" }),
                EasySetupType("SanatTasarim", "Sanat / Tasarım", "Tasarım, eser, atölye ve malzeme takibi için.", new[] { "Tasarım Geliri", "Eser Satışı", "Atölye Geliri" }, new[] { "Malzeme Giderleri", "Atölye Giderleri", "Baskı Giderleri", "Pazarlama Giderleri", "Komisyon Giderleri" }),
                EasySetupType("Franchise", "Franchise / Şube", "Şube satışı, royalty, merkez giderleri ve stok takibi için.", new[] { "Şube Satışı", "Royalty Geliri", "Ürün Satışı" }, new[] { "Royalty Giderleri", "Stok Alımları", "Personel Giderleri", "Kira Giderleri", "Merkez Katkı Payı" })
            };
        }

        private static KolayKurulumTurDto EasySetupType(string kod, string ad, string aciklama, IEnumerable<string> gelirKalemleri, IEnumerable<string> giderKalemleri)
        {
            return new KolayKurulumTurDto
            {
                kod = kod,
                ad = ad,
                aciklama = aciklama,
                gelirKalemleri = gelirKalemleri.ToList(),
                giderKalemleri = giderKalemleri.ToList()
            };
        }

        private TelegramEkranDto BuildTelegramScreen(string mesaj = "", bool? bagliOverride = null)
        {
            var pairing = _telegramPairingService?.EnsureActiveCode() ??
                new TelegramPairingCode("SC-000000", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(10));
            var botUsername = string.IsNullOrWhiteSpace(_telegramSettings?.BotUsername)
                ? "SystemcelBot"
                : _telegramSettings.BotUsername.Trim().TrimStart('@');
            var link = $"https://t.me/{Uri.EscapeDataString(botUsername)}?start={Uri.EscapeDataString(pairing.Code)}";
            var isConnected = bagliOverride ?? (_telegramSettings?.IsEnabled ?? false);

            return new TelegramEkranDto
            {
                bagli = isConnected,
                durum = isConnected ? "Bağlı" : "Bağlı değil",
                botKullaniciAdi = botUsername,
                eslestirmeKodu = pairing.Code,
                baglantiLinki = link,
                qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=260x260&data={Uri.EscapeDataString(link)}",
                gecerlilikDakika = pairing.MinutesLeft,
                mesaj = mesaj
            };
        }

        private async Task<AyarlarEkranDto> BuildSettingsScreenAsync(
            string mesaj = "",
            int? preferredBusinessId = null,
            int? preferredKalemId = null)
        {
            UseTurkishLanguage();

            var activeBusiness = await _isletmeService!.GetActiveAsync();
            var businesses = await _isletmeService!.GetAllAsync();
            var kalemler = await _kalemTanimiService!.GetAllAsync();
            var activeId = preferredBusinessId ?? activeBusiness.Id;

            return new AyarlarEkranDto
            {
                aktifIsletmeId = activeBusiness.Id,
                aktifIsletme = string.IsNullOrWhiteSpace(activeBusiness.Ad) ? "Bilinmiyor" : activeBusiness.Ad.Trim(),
                seciliIsletmeId = activeId,
                seciliKalemId = preferredKalemId,
                dil = "tr",
                diller = new List<AyarDilDto>
                {
                    new()
                    {
                        kod = "tr",
                        ad = "Türkçe"
                    }
                },
                isletmeler = businesses
                    .Select(x => new AyarIsletmeDto
                    {
                        id = x.Id,
                        ad = string.IsNullOrWhiteSpace(x.Ad) ? "İşletme" : x.Ad.Trim(),
                        aktif = x.IsAktif
                    })
                    .ToList(),
                kalemler = kalemler
                    .Select(x => new AyarKalemDto
                    {
                        id = x.Id,
                        tip = x.Tip,
                        ad = string.IsNullOrWhiteSpace(x.Ad) ? "Kalem" : x.Ad.Trim()
                    })
                    .ToList(),
                mesaj = mesaj
            };
        }

        private async Task<GibPortalEkranDto> BuildGibPortalScreenAsync()
        {
            var activeBusiness = await _isletmeService!.GetActiveAsync();
            var settings = await _gibPortalService!.GetSettingsAsync();

            return new GibPortalEkranDto
            {
                aktifIsletme = string.IsNullOrWhiteSpace(activeBusiness.Ad) ? "Bilinmiyor" : activeBusiness.Ad.Trim(),
                kullaniciKodu = settings?.KullaniciKodu ?? string.Empty,
                hasPassword = settings?.HasPassword ?? false,
                testModu = settings?.TestModu ?? false,
                mesaj = settings is null
                    ? "GİB Portal ayarları henüz yapılandırılmadı."
                    : "GİB Portal ayarları hazır."
            };
        }

        private async Task<ApiMesaj> SendDashboardSummaryToTelegramAsync()
        {
            if (_telegramSettings is null || !_telegramSettings.IsEnabled || _summaryService is null || _kasaService is null || _isletmeService is null || _backupReportService is null)
                return new ApiMesaj(AppLocalization.T("main.telegram.notConfigured"));

            var today = DateTime.Today;
            var rangeCode = SummaryRangeCatalog.Last30Days;
            var (from, to) = SummaryRangeCatalog.GetRange(rangeCode, today);
            var summary = await _summaryService!.GetSummaryAsync(from, to);
            var records = await _kasaService!.GetAllAsync(from, to);
            var activeBusiness = await _isletmeService!.GetActiveAsync();
            var businessName = string.IsNullOrWhiteSpace(activeBusiness.Ad)
                ? AppLocalization.T("common.unknown")
                : activeBusiness.Ad.Trim();

            var title = AppLocalization.F("main.telegram.dynamicTitle", SummaryRangeCatalog.GetDisplay(rangeCode, today));
            var text = BuildTelegramSummaryText(title, from, to, summary, records, businessName);
            await _backupReportService!.SendTextAsync(text);

            return new ApiMesaj(AppLocalization.T("main.telegram.sent"));
        }

        private async Task<ApiMesaj> SaveDashboardSummaryPdfAsync()
        {
            var report = await BuildDashboardPrintReportAsync();
            var path = SavePrintReportHtml(report, "yonetici-ozeti");
            return new ApiMesaj($"Web host PDF yerine yazdırılabilir HTML raporu hazırladı: {path}");
        }

        private async Task<PrintReportData> BuildDashboardPrintReportAsync()
        {
            var today = DateTime.Today;
            var rangeCode = SummaryRangeCatalog.Last30Days;
            var (from, to) = SummaryRangeCatalog.GetRange(rangeCode, today);
            var summary = await _summaryService!.GetSummaryAsync(from, to);
            var records = await _kasaService!.GetAllAsync(from, to);
            var activeBusiness = await _isletmeService!.GetActiveAsync();

            var request = new PrintReportRequest
            {
                Template = PrintReportTemplate.ExecutiveSummary,
                From = from,
                To = to,
                RangeDisplay = AppLocalization.F(
                    "print.range.named",
                    SummaryRangeCatalog.GetDisplay(rangeCode, today),
                    from,
                    to),
                Note = string.Empty,
                GeneratedAt = DateTime.Now,
                RecordLimit = null,
                IsPreview = false
            };

            return PrintReportComposer.Compose(request, activeBusiness.Ad, summary, records);
        }

        private async Task<RaporlarEkranDto> BuildReportsScreenAsync()
        {
            var activeBusiness = await _isletmeService!.GetActiveAsync();
            var today = DateTime.Today;

            return new RaporlarEkranDto
            {
                aktifIsletme = string.IsNullOrWhiteSpace(activeBusiness.Ad) ? "Bilinmiyor" : activeBusiness.Ad.Trim(),
                bugun = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                varsayilanDonem = today.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                varsayilanKlasor = GetDefaultReportsFolder(),
                formatlar = new List<RaporSecimDto>
                {
                    new("excel", "Excel", false),
                    new("html", "HTML", false),
                    new("zip", "ZIP", true)
                },
                icerikler = new List<RaporSecimDto>
                {
                    new("faturalar", "Faturalar", false),
                    new("cari", "Cari Hesaplar", false),
                    new("stok", "Stok", false),
                    new("gelirGider", "Gelir / Gider", false),
                    new("kdv", "KDV Özeti", false)
                },
                yazdirmaSablonlari = PrintReportComposer.CreateTemplateOptions()
                    .Select(x => new SecenekDto(ToReportTemplateCode(x.Template), x.Display))
                    .ToList(),
                tarihAraliklari = PrintRangeCatalog.CreateLocalizedOptions(today)
                    .Select(x => new SecenekDto(x.Code, x.Display))
                    .ToList(),
                sonPaket = _lastReportPackage
            };
        }

        private async Task<RaporPaketDto> CreateReportPackageAsync(RaporPaketOlusturIstek request)
        {
            if (request is null)
                throw new ArgumentException("Rapor bilgileri alınamadı.");

            if (!TryParseReportMonth(request.donem, out var month))
                throw new ArgumentException("Dönem yyyy-MM formatında olmalıdır.");

            var options = CreateMonthlyReportOptions(request);
            var outputDirectory = string.IsNullOrWhiteSpace(request.klasor)
                ? GetDefaultReportsFolder()
                : request.klasor.Trim();

            Directory.CreateDirectory(outputDirectory);
            var reportPath = await _onMuhasebeReportService!.CreateMonthlyExportAsync(month, outputDirectory, options);
            _lastReportPackage = ToReportPackageDto(reportPath, month);
            return _lastReportPackage;
        }

        private static MonthlyReportExportOptions CreateMonthlyReportOptions(RaporPaketOlusturIstek request)
        {
            var formats = (request.formatlar ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (formats.Count == 0)
                throw new ArgumentException("En az bir dış aktarım formatı seçin.");

            var wantsZip = formats.Contains("zip");
            var contents = (request.icerikler ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var includeAllContents = contents.Count == 0;

            return new MonthlyReportExportOptions
            {
                CreateZip = wantsZip,
                IncludeExcel = wantsZip || formats.Contains("excel") || formats.Contains("csv"),
                IncludeHtml = wantsZip || formats.Contains("html"),
                IncludeFaturalar = includeAllContents || contents.Contains("faturalar"),
                IncludeCari = includeAllContents || contents.Contains("cari"),
                IncludeStok = includeAllContents || contents.Contains("stok"),
                IncludeGelirGider = includeAllContents || contents.Contains("gelirGider"),
                IncludeKdv = includeAllContents || contents.Contains("kdv")
            };
        }

        private async Task<ApiMesaj> SaveReportPdfAsync(RaporYazdirIstek request)
        {
            var report = await BuildPrintReportAsync(request, recordLimit: null, isPreview: false);
            var path = SavePrintReportHtml(report);
            return new ApiMesaj($"Web host PDF yerine yazdırılabilir HTML raporu hazırladı: {path}");
        }

        private static Task<string> SelectReportFolderAsync(string? currentPath)
        {
            var selected = Directory.Exists(currentPath) ? currentPath! : GetDefaultReportsFolder();
            Directory.CreateDirectory(selected);
            return Task.FromResult(selected);
        }

        private async Task<ApiMesaj> ExportReportHtmlAsync(RaporYazdirIstek request)
        {
            var report = await BuildPrintReportAsync(request, recordLimit: null, isPreview: false);
            var path = SavePrintReportHtml(report);
            return new ApiMesaj($"HTML kaydedildi: {path}");
        }

        private Task<ApiMesaj> PrintReportAsync(RaporYazdirIstek request)
        {
            return Task.FromResult(new ApiMesaj("Yazdırma işlemi web akışında tarayıcının yazdır penceresiyle tamamlanacak."));
        }

        private async Task<PrintReportData> BuildPrintReportAsync(RaporYazdirIstek request, int? recordLimit, bool isPreview)
        {
            var printRequest = CreatePrintReportRequest(request, recordLimit, isPreview);
            var summary = await _summaryService!.GetSummaryAsync(printRequest.From, printRequest.To);
            var records = await _kasaService!.GetAllAsync(printRequest.From, printRequest.To);
            var activeBusiness = await _isletmeService!.GetActiveAsync();
            return PrintReportComposer.Compose(printRequest, activeBusiness.Ad, summary, records);
        }

        private static PrintReportRequest CreatePrintReportRequest(RaporYazdirIstek request, int? recordLimit, bool isPreview)
        {
            request ??= new RaporYazdirIstek();
            var template = FromReportTemplateCode(request.sablon);
            var rangeCode = PrintRangeCatalog.NormalizeCode(request.aralikKodu, SummaryRangeCatalog.Monthly);
            DateTime from;
            DateTime to;
            string rangeDisplay;

            if (string.Equals(rangeCode, PrintRangeCatalog.Custom, StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseDate(request.baslangic, out from) || !TryParseDate(request.bitis, out to))
                    throw new ArgumentException("Özel aralık için başlangıç ve bitiş tarihi seçin.");

                if (from > to)
                    throw new ArgumentException("Başlangıç tarihi bitiş tarihinden sonra olamaz.");

                rangeDisplay = AppLocalization.F("print.range.between", from, to);
            }
            else
            {
                (from, to) = SummaryRangeCatalog.GetRange(rangeCode, DateTime.Today);
                rangeDisplay = AppLocalization.F(
                    "print.range.named",
                    PrintRangeCatalog.GetDisplay(rangeCode, DateTime.Today),
                    from,
                    to);
            }

            return new PrintReportRequest
            {
                Template = template,
                From = from,
                To = to,
                RangeDisplay = rangeDisplay,
                Note = request.notMetni ?? string.Empty,
                GeneratedAt = DateTime.Now,
                RecordLimit = recordLimit,
                IsPreview = isPreview
            };
        }

        private static string SavePrintReportHtml(PrintReportData report, string? fileNamePrefix = null)
        {
            var outputDirectory = GetDefaultReportsFolder();
            Directory.CreateDirectory(outputDirectory);
            var fileName = string.IsNullOrWhiteSpace(fileNamePrefix)
                ? BuildReportExportFileName(report.Template, "html")
                : $"systemcel-{fileNamePrefix}-{DateTime.Now:yyyyMMdd-HHmm}.html";
            var path = Path.Combine(outputDirectory, fileName);
            File.WriteAllText(path, PrintReportHtmlExporter.Generate(report), Encoding.UTF8);
            return path;
        }

        private static string BuildReportExportFileName(PrintReportTemplate template, string extension)
        {
            var templateName = template == PrintReportTemplate.AccountingReport
                ? "muhasebe-raporu"
                : "yonetici-ozeti";
            return $"systemcel-{templateName}-{DateTime.Now:yyyyMMdd-HHmm}.{extension}";
        }

        private static void OpenReportPath(string? rawPath, bool selectInFolder)
        {
            var path = string.IsNullOrWhiteSpace(rawPath) ? string.Empty : rawPath.Trim();
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Açılacak rapor yolu bulunamadı.");

            if (File.Exists(path))
            {
                if (selectInFolder)
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
                    return;
                }

                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                return;
            }

            if (Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
                return;
            }

            throw new FileNotFoundException("Rapor yolu bulunamadı.", path);
        }

        private static RaporPaketDto ToReportPackageDto(string zipPath, DateTime month)
        {
            if (Directory.Exists(zipPath))
            {
                var directory = new DirectoryInfo(zipPath);
                return new RaporPaketDto
                {
                    varMi = true,
                    ad = directory.Name,
                    yol = directory.FullName,
                    klasor = directory.Parent?.FullName ?? directory.FullName,
                    donem = month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    olusturmaZamani = directory.LastWriteTime.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture)
                };
            }

            var file = new FileInfo(zipPath);
            return new RaporPaketDto
            {
                varMi = file.Exists,
                ad = file.Name,
                yol = file.FullName,
                klasor = file.DirectoryName ?? string.Empty,
                donem = month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                olusturmaZamani = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            };
        }

        private static bool TryParseReportMonth(string? value, out DateTime month)
        {
            if (DateTime.TryParseExact(value?.Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out month))
            {
                month = new DateTime(month.Year, month.Month, 1);
                return true;
            }

            if (TryParseDate(value, out month))
            {
                month = new DateTime(month.Year, month.Month, 1);
                return true;
            }

            month = default;
            return false;
        }

        private static string GetDefaultReportsFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Systemcel",
                "Raporlar");
        }

        private static string ToReportTemplateCode(PrintReportTemplate template)
        {
            return template == PrintReportTemplate.AccountingReport ? "muhasebeRaporu" : "yoneticiOzeti";
        }

        private static PrintReportTemplate FromReportTemplateCode(string? value)
        {
            return NormalizeAscii(value) switch
            {
                "accounting" or "accountingreport" or "muhaseberaporu" => PrintReportTemplate.AccountingReport,
                _ => PrintReportTemplate.ExecutiveSummary
            };
        }

        private async Task<CariEkranDto> BuildCariScreenAsync()
        {
            var activeBusiness = await _isletmeService!.GetActiveAsync();
            var cards = await _cariService!.GetAllAsync();

            return new CariEkranDto
            {
                aktifIsletme = string.IsNullOrWhiteSpace(activeBusiness.Ad) ? "Bilinmiyor" : activeBusiness.Ad.Trim(),
                kartlar = cards
                    .OrderBy(x => x.Id)
                    .Select(ToCariListItemDto)
                    .ToList(),
                tipSecenekleri = new List<SecenekDto>
                {
                    new("Musteri", "Müşteri"),
                    new("Tedarikci", "Tedarikçi"),
                    new("HerIkisi", "Her İkisi")
                },
                hareketTipleri = new List<SecenekDto>
                {
                    new("Borc", "Borç"),
                    new("Alacak", "Alacak"),
                    new("Tahsilat", "Tahsilat"),
                    new("Odeme", "Ödeme")
                }
            };
        }

        private async Task<CariDetayDto?> BuildCariDetailAsync(int id)
        {
            var row = await _cariService!.GetByIdAsync(id);
            if (row is null)
                return null;

            var balance = await _cariService!.GetBakiyeAsync(id);
            var movements = await _cariService!.GetHareketlerAsync(id);

            return new CariDetayDto
            {
                kart = ToCariFormDto(row),
                bakiye = balance,
                hareketler = movements
                    .OrderByDescending(x => x.Tarih)
                    .ThenByDescending(x => x.Id)
                    .Select(ToCariMovementDto)
                    .ToList()
            };
        }

        private void UseTurkishLanguage()
        {
            AppLocalization.SetLanguage("tr");
            CultureInfo.DefaultThreadCurrentCulture = AppLocalization.CurrentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = AppLocalization.CurrentCulture;

            if (_runtimeOptions is null)
                return;

            var state = AppStateStore.Load(_runtimeOptions.AppDataPath);
            if (string.Equals(state.LanguageCode, "tr", StringComparison.OrdinalIgnoreCase))
                return;

            state.LanguageCode = "tr";
            AppStateStore.Save(_runtimeOptions.AppDataPath, state);
        }

        private async Task<UrunStokEkranDto> BuildProductStockScreenAsync()
        {
            var activeBusiness = await _isletmeService!.GetActiveAsync();
            var products = await _urunHizmetService!.GetAllAsync();
            var productRows = new List<UrunListeSatirDto>(products.Count);

            foreach (var product in products.OrderByDescending(x => x.Aktif).ThenBy(x => x.Ad, StringComparer.OrdinalIgnoreCase))
                productRows.Add(await ToProductRowDtoAsync(product));

            var productNames = products.ToDictionary(x => x.Id, x => x.Ad);
            var movements = await _stokService!.GetRecentMovementsAsync(12);

            return new UrunStokEkranDto
            {
                aktifIsletme = string.IsNullOrWhiteSpace(activeBusiness.Ad) ? "Bilinmiyor" : activeBusiness.Ad.Trim(),
                urunler = productRows,
                sonHareketler = movements
                    .Select(x => ToStockMovementDto(x, productNames))
                    .ToList(),
                tipSecenekleri = new List<SecenekDto>
                {
                    new("Urun", "Ürün"),
                    new("Hizmet", "Hizmet")
                },
                birimSecenekleri = new List<SecenekDto>
                {
                    new("Adet", "Adet"),
                    new("Saat", "Saat"),
                    new("Kg", "Kg"),
                    new("Lt", "Lt"),
                    new("Paket", "Paket"),
                    new("Metre", "Metre")
                }
            };
        }

        private async Task<FaturaEkranDto> BuildInvoiceScreenAsync()
        {
            var activeBusiness = await _isletmeService!.GetActiveAsync();
            var invoices = await _faturaService!.GetAllAsync();
            var cariler = await _cariService!.GetAllAsync();
            var urunler = await _urunHizmetService!.GetAllAsync();
            var cariNames = cariler.ToDictionary(x => x.Id, x => (x.Unvan ?? string.Empty).Trim());
            var invoiceRows = invoices
                .OrderByDescending(x => x.Tarih)
                .ThenByDescending(x => x.Id)
                .Select(x => ToInvoiceRowDto(x, cariNames))
                .ToList();
            var today = DateTime.Today;

            return new FaturaEkranDto
            {
                aktifIsletme = string.IsNullOrWhiteSpace(activeBusiness.Ad) ? "Bilinmiyor" : activeBusiness.Ad.Trim(),
                faturalar = invoiceRows,
                cariler = cariler
                    .Where(x => x.Aktif)
                    .OrderBy(x => x.Unvan, StringComparer.OrdinalIgnoreCase)
                    .Select(x => new FaturaCariSecenekDto
                    {
                        id = x.Id,
                        unvan = string.IsNullOrWhiteSpace(x.Unvan) ? $"Cari #{x.Id}" : x.Unvan.Trim()
                    })
                    .ToList(),
                urunler = urunler
                    .Where(x => x.Aktif)
                    .OrderBy(x => x.Ad, StringComparer.OrdinalIgnoreCase)
                    .Select(ToInvoiceProductOptionDto)
                    .ToList(),
                ozet = new FaturaOzetDto
                {
                    toplamFatura = invoiceRows.Sum(x => x.genelToplam),
                    faturaAdedi = invoiceRows.Count,
                    tahsilEdilen = invoiceRows.Sum(x => x.odenenTutar),
                    bekleyen = invoiceRows.Sum(x => Math.Max(0m, x.genelToplam - x.odenenTutar)),
                    bekleyenAdedi = invoiceRows.Count(x => x.durum is not FaturaDurum.Odendi and not FaturaDurum.Iptal && x.genelToplam > x.odenenTutar)
                },
                faturaTipleri = new List<SecenekDto>
                {
                    new("Satis", "Satış"),
                    new("Alis", "Alış")
                },
                odemeYontemleri = new List<SecenekDto>
                {
                    new("Nakit", "Nakit"),
                    new("KrediKarti", "Kredi Kartı"),
                    new("OnlineOdeme", "Online Ödeme"),
                    new("Havale", "Havale")
                },
                bugun = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
        }

        private async Task<FaturaDetayDto?> BuildInvoiceDetailAsync(int id)
        {
            var detail = await _faturaService!.GetDetailAsync(id);
            if (detail is null)
                return null;

            return ToInvoiceDetailDto(detail);
        }

        private async Task<TahsilatOdemeEkranDto> BuildPaymentScreenAsync()
        {
            var activeBusiness = await _isletmeService!.GetActiveAsync();
            var cariler = await _cariService!.GetAllAsync();
            var cariNames = cariler.ToDictionary(x => x.Id, x => (x.Unvan ?? string.Empty).Trim());
            var hareketler = new List<TahsilatOdemeListeSatirDto>();

            foreach (var cari in cariler)
            {
                var cariHareketleri = await _cariService.GetHareketlerAsync(cari.Id);
                hareketler.AddRange(cariHareketleri
                    .Where(x => NormalizeCariMovementType(x.HareketTipi) is "Tahsilat" or "Odeme")
                    .Select(x => ToPaymentMovementRowDto(x, cari)));
            }

            var siraliHareketler = hareketler
                .OrderByDescending(x => x.tarih)
                .ThenByDescending(x => x.id)
                .Take(80)
                .ToList();

            var faturalar = _faturaService is null
                ? new List<Fatura>()
                : await _faturaService.GetAllAsync();

            var bekleyenFaturalar = faturalar
                .Where(x => x.Durum is not FaturaDurum.Odendi and not FaturaDurum.Iptal)
                .ToList();

            var listeSatirlari = siraliHareketler
                .Concat(bekleyenFaturalar.Select(x => ToPaymentPendingInvoiceRowDto(x, cariNames)))
                .OrderByDescending(x => x.tarih)
                .ThenByDescending(x => x.id)
                .Take(80)
                .ToList();

            return new TahsilatOdemeEkranDto
            {
                aktifIsletme = string.IsNullOrWhiteSpace(activeBusiness.Ad) ? "Bilinmiyor" : activeBusiness.Ad.Trim(),
                hareketler = listeSatirlari,
                cariler = cariler
                    .Where(x => x.Aktif)
                    .OrderBy(x => x.Unvan, StringComparer.OrdinalIgnoreCase)
                    .Select(x => new FaturaCariSecenekDto
                    {
                        id = x.Id,
                        unvan = string.IsNullOrWhiteSpace(x.Unvan) ? $"Cari #{x.Id}" : x.Unvan.Trim()
                    })
                    .ToList(),
                faturalar = bekleyenFaturalar
                    .OrderByDescending(x => x.Tarih)
                    .ThenByDescending(x => x.Id)
                    .Select(x => ToPaymentInvoiceOptionDto(x, cariNames))
                    .ToList(),
                ozet = new TahsilatOdemeOzetDto
                {
                    toplamTahsilat = siraliHareketler.Where(x => x.tip == "Tahsilat").Sum(x => x.tutar),
                    tahsilatAdedi = siraliHareketler.Count(x => x.tip == "Tahsilat"),
                    toplamOdeme = siraliHareketler.Where(x => x.tip == "Odeme").Sum(x => x.tutar),
                    odemeAdedi = siraliHareketler.Count(x => x.tip == "Odeme"),
                    bekleyen = bekleyenFaturalar.Sum(x => Math.Max(0m, x.GenelToplam - x.OdenenTutar)),
                    bekleyenAdedi = bekleyenFaturalar.Count
                },
                islemTipleri = new List<SecenekDto>
                {
                    new("Tahsilat", "Tahsilat"),
                    new("Odeme", "Ödeme")
                },
                odemeYontemleri = new List<SecenekDto>
                {
                    new("Nakit", "Nakit"),
                    new("KrediKarti", "Kredi Kartı"),
                    new("OnlineOdeme", "Online Ödeme"),
                    new("Havale", "Havale")
                },
                paraBirimleri = new List<SecenekDto>
                {
                    new("TRY", "TL"),
                    new("USD", "USD"),
                    new("EUR", "EUR")
                },
                kategoriler = new List<SecenekDto>
                {
                    new("Genel", "Genel"),
                    new("Fatura", "Fatura"),
                    new("Nakit", "Nakit"),
                    new("Banka", "Banka")
                },
                bugun = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
        }

        private async Task<GelirGiderEkranDto> BuildCashflowAsync()
        {
            var activeBusiness = await _isletmeService!.GetActiveAsync();
            var records = (await _kasaService!.GetAllAsync())
                .Select(ToKayitDto)
                .ToList();
            var incomeCategories = await GetCategoriesAsync("Gelir");
            var expenseCategories = await GetCategoriesAsync("Gider");
            var products = (await _urunHizmetService!.GetAllAsync())
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
            return (await _kalemTanimiService!.GetByTipAsync(tur))
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

                var product = await _urunHizmetService!.GetByIdAsync(stock.urunId);
                if (product is null || !product.Aktif || !string.Equals(product.Tip, "Urun", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Stok girişi için geçerli ürün seçin.");

                await _stokService!.CreateMovementAsync(new StokHareketCreateRequest
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
                await _kasaService!.DeleteAsync(kayitId);
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
                return new ApiHata("Tur alani gelir veya gider olmalidir.");

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

        private static ApiHata? ValidateCariRequest(CariKaydetIstek request)
        {
            if (request is null)
                return new ApiHata("Cari kart bilgileri alınamadı.");

            if (string.IsNullOrWhiteSpace(NormalizeCariTip(request.tip)))
                return new ApiHata("Cari tipini seçin.");

            if (string.IsNullOrWhiteSpace(request.unvan))
                return new ApiHata("Unvan alanı zorunludur.");

            return null;
        }

        private static ApiHata? ValidateCariMovementRequest(CariHareketKaydetIstek request)
        {
            if (request is null)
                return new ApiHata("Cari hareket bilgileri alınamadı.");

            if (string.IsNullOrWhiteSpace(NormalizeCariMovementType(request.hareketTipi)))
                return new ApiHata("Hareket tipini seçin.");

            if (request.tutar <= 0)
                return new ApiHata("Tutar sıfırdan büyük olmalıdır.");

            if (!TryParseDate(request.tarih, out _))
                return new ApiHata("Tarih geçerli değil.");

            return null;
        }

        private static ApiHata? ValidateProductRequest(UrunHizmetKaydetIstek request)
        {
            if (request is null)
                return new ApiHata("Ürün/hizmet bilgileri alınamadı.");

            if (NormalizeProductType(request.tip) is not ("Urun" or "Hizmet"))
                return new ApiHata("Tip alanı ürün veya hizmet olmalıdır.");

            if (string.IsNullOrWhiteSpace(request.ad))
                return new ApiHata("Ad alanı zorunludur.");

            if (request.kdvOrani < 0 || request.alisFiyati < 0 || request.satisFiyati < 0 || request.kritikStok < 0)
                return new ApiHata("Fiyat, KDV ve kritik stok negatif olamaz.");

            return null;
        }

        private static ApiHata? ValidateStockMovementRequest(StokHareketKaydetIstek request)
        {
            if (request is null)
                return new ApiHata("Stok hareket bilgileri alınamadı.");

            if (request.miktar == 0)
                return new ApiHata("Miktar sıfır olamaz.");

            if (!TryParseDate(request.tarih, out _))
                return new ApiHata("Tarih geçerli değil.");

            return null;
        }

        private static ApiHata? ValidateInvoiceRequest(FaturaKaydetIstek request)
        {
            if (request is null)
                return new ApiHata("Fatura bilgileri alınamadı.");

            if (request.cariKartId <= 0)
                return new ApiHata("Cari seçin.");

            if (NormalizeInvoiceType(request.faturaTipi) is not ("Satis" or "Alis"))
                return new ApiHata("Fatura tipini seçin.");

            if (!TryParseDate(request.tarih, out _))
                return new ApiHata("Tarih geçerli değil.");

            if (!string.IsNullOrWhiteSpace(request.vadeTarihi) && !TryParseDate(request.vadeTarihi, out _))
                return new ApiHata("Vade tarihi geçerli değil.");

            if (request.satirlar.Count == 0)
                return new ApiHata("En az bir fatura satırı girin.");

            foreach (var row in request.satirlar)
            {
                if (row.miktar <= 0)
                    return new ApiHata("Miktar sıfırdan büyük olmalıdır.");

                if (row.birimFiyat < 0 || row.kdvOrani < 0 || row.iskontoOrani < 0)
                    return new ApiHata("Fiyat, KDV ve iskonto negatif olamaz.");
            }

            return null;
        }

        private static ApiHata? ValidateInvoicePaymentRequest(FaturaTahsilatKaydetIstek request)
        {
            if (request is null)
                return new ApiHata("Tahsilat/ödeme bilgileri alınamadı.");

            if (request.tutar <= 0)
                return new ApiHata("Tutar sıfırdan büyük olmalıdır.");

            if (!TryParseDate(request.tarih, out _))
                return new ApiHata("Tarih geçerli değil.");

            return null;
        }

        private static ApiHata? ValidateGibSettingsRequest(GibPortalAyarKaydetIstek request, bool requirePassword)
        {
            if (request is null)
                return new ApiHata("GİB Portal bilgileri alınamadı.");

            if (string.IsNullOrWhiteSpace(request.kullaniciKodu))
                return new ApiHata("GİB kullanıcı kodunu girin.");

            if (requirePassword && string.IsNullOrWhiteSpace(request.sifre))
                return new ApiHata("GİB şifresini girin.");

            return null;
        }

        private static ApiHata? ValidatePaymentScreenRequest(TahsilatOdemeKaydetIstek request)
        {
            if (request is null)
                return new ApiHata("Tahsilat/ödeme bilgileri alınamadı.");

            var movementType = NormalizeCariMovementType(request.islemTipi);
            if (movementType is not ("Tahsilat" or "Odeme"))
                return new ApiHata("İşlem tipi tahsilat veya ödeme olmalıdır.");

            if (request.cariKartId <= 0)
                return new ApiHata("Cari seçin.");

            if (request.tutar <= 0)
                return new ApiHata("Tutar sıfırdan büyük olmalıdır.");

            if (!TryParseDate(request.tarih, out _))
                return new ApiHata("Tarih geçerli değil.");

            if (!string.IsNullOrWhiteSpace(request.vadeTarihi) && !TryParseDate(request.vadeTarihi, out _))
                return new ApiHata("Vade tarihi geçerli değil.");

            if (request.faturaIleEslestir && request.faturaId <= 0)
                return new ApiHata("Fatura ile eşleştirmek için belge/fatura seçin.");

            return null;
        }

        private static CariKart ToDomainCari(CariKaydetIstek request)
        {
            return new CariKart
            {
                Id = request.id ?? 0,
                Tip = NormalizeCariTip(request.tip),
                Unvan = (request.unvan ?? string.Empty).Trim(),
                Telefon = (request.telefon ?? string.Empty).Trim(),
                Eposta = (request.eposta ?? string.Empty).Trim(),
                VergiNoTc = (request.vergiNoTc ?? string.Empty).Trim(),
                VergiDairesi = (request.vergiDairesi ?? string.Empty).Trim(),
                Adres = (request.adres ?? string.Empty).Trim(),
                Aktif = request.aktif
            };
        }

        private static CariHareket ToDomainCariMovement(int cariKartId, CariHareketKaydetIstek request)
        {
            return new CariHareket
            {
                CariKartId = cariKartId,
                Tarih = ParseDate(request.tarih),
                HareketTipi = NormalizeCariMovementType(request.hareketTipi),
                Tutar = request.tutar,
                Kaynak = "Manuel",
                Aciklama = string.IsNullOrWhiteSpace(request.aciklama) ? null : request.aciklama.Trim()
            };
        }

        private static UrunHizmetCreateRequest ToProductCreateRequest(UrunHizmetKaydetIstek request)
        {
            return new UrunHizmetCreateRequest
            {
                Tip = NormalizeProductType(request.tip),
                Ad = (request.ad ?? string.Empty).Trim(),
                Barkod = (request.barkod ?? string.Empty).Trim(),
                Birim = string.IsNullOrWhiteSpace(request.birim) ? "Adet" : request.birim.Trim(),
                KdvOrani = request.kdvOrani,
                AlisFiyati = request.alisFiyati,
                SatisFiyati = request.satisFiyati,
                KritikStok = request.kritikStok
            };
        }

        private static UrunHizmet ToDomainProduct(UrunHizmetKaydetIstek request)
        {
            return new UrunHizmet
            {
                Id = request.id ?? 0,
                Tip = NormalizeProductType(request.tip),
                Ad = (request.ad ?? string.Empty).Trim(),
                Barkod = (request.barkod ?? string.Empty).Trim(),
                Birim = string.IsNullOrWhiteSpace(request.birim) ? "Adet" : request.birim.Trim(),
                KdvOrani = request.kdvOrani,
                AlisFiyati = request.alisFiyati,
                SatisFiyati = request.satisFiyati,
                KritikStok = request.kritikStok,
                Aktif = request.aktif
            };
        }

        private static FaturaCreateRequest ToInvoiceCreateRequest(FaturaKaydetIstek request)
        {
            return new FaturaCreateRequest
            {
                CariKartId = request.cariKartId,
                Tarih = ParseDate(request.tarih),
                VadeTarihi = string.IsNullOrWhiteSpace(request.vadeTarihi) ? null : ParseDate(request.vadeTarihi),
                FaturaTipi = NormalizeInvoiceType(request.faturaTipi),
                OdemeYontemi = ToDomainPayment(request.odemeYontemi),
                Aciklama = string.IsNullOrWhiteSpace(request.aciklama) ? null : request.aciklama.Trim(),
                Satirlar = request.satirlar.Select(x => new FaturaSatirRequest
                {
                    UrunHizmetId = x.urunHizmetId > 0 ? x.urunHizmetId : null,
                    Aciklama = (x.aciklama ?? string.Empty).Trim(),
                    Birim = string.IsNullOrWhiteSpace(x.birim) ? "Adet" : x.birim.Trim(),
                    Miktar = x.miktar,
                    BirimFiyat = x.birimFiyat,
                    IskontoOrani = x.iskontoOrani,
                    KdvOrani = x.kdvOrani,
                    StokEtkilesin = x.stokEtkilesin
                }).ToList()
            };
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

        private static KayitDto ToKayitDto(Kasa row)
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

        private static CariListeSatirDto ToCariListItemDto(CariKart row)
        {
            return new CariListeSatirDto
            {
                id = row.Id,
                tip = ToCariTipLabel(row.Tip),
                unvan = (row.Unvan ?? string.Empty).Trim(),
                telefon = (row.Telefon ?? string.Empty).Trim(),
                vergiNo = (row.VergiNoTc ?? string.Empty).Trim(),
                aktif = row.Aktif
            };
        }

        private static CariKartFormDto ToCariFormDto(CariKart row)
        {
            var tip = NormalizeCariTip(row.Tip);
            return new CariKartFormDto
            {
                id = row.Id,
                tip = string.IsNullOrWhiteSpace(tip) ? "Musteri" : tip,
                unvan = (row.Unvan ?? string.Empty).Trim(),
                telefon = (row.Telefon ?? string.Empty).Trim(),
                eposta = (row.Eposta ?? string.Empty).Trim(),
                vergiNoTc = (row.VergiNoTc ?? string.Empty).Trim(),
                vergiDairesi = (row.VergiDairesi ?? string.Empty).Trim(),
                adres = (row.Adres ?? string.Empty).Trim(),
                aktif = row.Aktif
            };
        }

        private static CariHareketDto ToCariMovementDto(CariHareket row)
        {
            return new CariHareketDto
            {
                id = row.Id,
                tarih = row.Tarih.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                hareketTipi = ToCariMovementLabel(row.HareketTipi),
                kaynak = (row.Kaynak ?? string.Empty).Trim(),
                aciklama = (row.Aciklama ?? string.Empty).Trim(),
                tutar = row.Tutar
            };
        }

        private async Task<UrunListeSatirDto> ToProductRowDtoAsync(UrunHizmet row)
        {
            var currentStock = string.Equals(row.Tip, "Urun", StringComparison.OrdinalIgnoreCase)
                ? await _stokService!.GetCurrentStockAsync(row.Id)
                : 0m;

            return new UrunListeSatirDto
            {
                id = row.Id,
                tip = NormalizeProductType(row.Tip),
                ad = (row.Ad ?? string.Empty).Trim(),
                barkod = (row.Barkod ?? string.Empty).Trim(),
                birim = (row.Birim ?? string.Empty).Trim(),
                kdvOrani = row.KdvOrani,
                alisFiyati = row.AlisFiyati,
                satisFiyati = row.SatisFiyati,
                kritikStok = row.KritikStok,
                mevcutStok = currentStock,
                aktif = row.Aktif
            };
        }

        private static FaturaListeSatirDto ToInvoiceRowDto(Fatura row, IReadOnlyDictionary<int, string> cariNames)
        {
            cariNames.TryGetValue(row.CariKartId, out var cariName);
            return new FaturaListeSatirDto
            {
                id = row.Id,
                no = string.IsNullOrWhiteSpace(row.PortalBelgeNo) ? row.YerelFaturaNo : row.PortalBelgeNo,
                tarih = row.Tarih.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                vadeTarihi = row.VadeTarihi?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
                faturaTipi = NormalizeInvoiceType(row.FaturaTipi),
                durum = string.IsNullOrWhiteSpace(row.Durum) ? FaturaDurum.YerelTaslak : row.Durum.Trim(),
                cariKartId = row.CariKartId,
                cariUnvan = string.IsNullOrWhiteSpace(cariName) ? $"Cari #{row.CariKartId}" : cariName.Trim(),
                genelToplam = row.GenelToplam,
                odenenTutar = row.OdenenTutar,
                odemeYontemi = ToDomainPayment(row.OdemeYontemi),
                aciklama = row.Aciklama ?? string.Empty
            };
        }

        private static FaturaDetayDto ToInvoiceDetailDto(FaturaDetail detail)
        {
            var row = detail.Fatura;
            return new FaturaDetayDto
            {
                fatura = new FaturaFormDto
                {
                    id = row.Id,
                    cariKartId = row.CariKartId,
                    tarih = row.Tarih.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    vadeTarihi = row.VadeTarihi?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
                    faturaTipi = NormalizeInvoiceType(row.FaturaTipi),
                    durum = string.IsNullOrWhiteSpace(row.Durum) ? FaturaDurum.YerelTaslak : row.Durum.Trim(),
                    yerelFaturaNo = row.YerelFaturaNo,
                    portalBelgeNo = row.PortalBelgeNo,
                    portalUuid = row.PortalUuid,
                    araToplam = row.AraToplam,
                    iskontoToplam = row.IskontoToplam,
                    kdvToplam = row.KdvToplam,
                    genelToplam = row.GenelToplam,
                    odenenTutar = row.OdenenTutar,
                    odemeYontemi = ToDomainPayment(row.OdemeYontemi),
                    aciklama = row.Aciklama ?? string.Empty,
                    cariUnvan = detail.Cari?.Unvan ?? string.Empty
                },
                satirlar = detail.Satirlar
                    .OrderBy(x => x.Id)
                    .Select(x => new FaturaSatirDto
                    {
                        id = x.Id,
                        urunHizmetId = x.UrunHizmetId ?? 0,
                        aciklama = x.Aciklama ?? string.Empty,
                        birim = x.Birim,
                        miktar = x.Miktar,
                        birimFiyat = x.BirimFiyat,
                        iskontoOrani = x.IskontoOrani,
                        kdvOrani = x.KdvOrani,
                        stokEtkilesin = x.StokEtkilesin,
                        satirToplam = x.SatirToplam
                    })
                    .ToList()
            };
        }

        private static FaturaUrunSecenekDto ToInvoiceProductOptionDto(UrunHizmet row)
        {
            return new FaturaUrunSecenekDto
            {
                id = row.Id,
                ad = string.IsNullOrWhiteSpace(row.Ad) ? $"Ürün #{row.Id}" : row.Ad.Trim(),
                tip = NormalizeProductType(row.Tip),
                birim = string.IsNullOrWhiteSpace(row.Birim) ? "Adet" : row.Birim.Trim(),
                kdvOrani = row.KdvOrani,
                alisFiyati = row.AlisFiyati,
                satisFiyati = row.SatisFiyati
            };
        }

        private static TahsilatOdemeListeSatirDto ToPaymentMovementRowDto(CariHareket row, CariKart cari)
        {
            var tip = NormalizeCariMovementType(row.HareketTipi);
            return new TahsilatOdemeListeSatirDto
            {
                id = row.Id,
                no = $"HRK-{row.Tarih:yyyy}-{row.Id:00000}",
                tarih = row.Tarih.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                tip = tip is "Odeme" ? "Odeme" : "Tahsilat",
                cariKartId = row.CariKartId,
                cariUnvan = string.IsNullOrWhiteSpace(cari.Unvan) ? $"Cari #{row.CariKartId}" : cari.Unvan.Trim(),
                odemeYontemi = ExtractPaymentMethod(row.Aciklama),
                tutar = row.Tutar,
                durum = "Tamamlandi",
                kaynak = string.IsNullOrWhiteSpace(row.Kaynak) ? "Manuel" : row.Kaynak.Trim(),
                aciklama = ExtractVisiblePaymentNote(row.Aciklama)
            };
        }

        private static TahsilatOdemeFaturaSecenekDto ToPaymentInvoiceOptionDto(Fatura row, IReadOnlyDictionary<int, string> cariNames)
        {
            cariNames.TryGetValue(row.CariKartId, out var cariName);
            var no = string.IsNullOrWhiteSpace(row.PortalBelgeNo) ? row.YerelFaturaNo : row.PortalBelgeNo;
            var kalan = Math.Max(0m, row.GenelToplam - row.OdenenTutar);

            return new TahsilatOdemeFaturaSecenekDto
            {
                id = row.Id,
                no = string.IsNullOrWhiteSpace(no) ? $"Fatura #{row.Id}" : no,
                cariKartId = row.CariKartId,
                cariUnvan = string.IsNullOrWhiteSpace(cariName) ? $"Cari #{row.CariKartId}" : cariName.Trim(),
                faturaTipi = NormalizeInvoiceType(row.FaturaTipi),
                durum = row.Durum,
                genelToplam = row.GenelToplam,
                odenenTutar = row.OdenenTutar,
                kalan = kalan,
                odemeYontemi = ToDomainPayment(row.OdemeYontemi),
                aciklama = string.IsNullOrWhiteSpace(row.Aciklama) ? string.Empty : row.Aciklama.Trim()
            };
        }

        private static TahsilatOdemeListeSatirDto ToPaymentPendingInvoiceRowDto(Fatura row, IReadOnlyDictionary<int, string> cariNames)
        {
            cariNames.TryGetValue(row.CariKartId, out var cariName);
            var invoiceNo = string.IsNullOrWhiteSpace(row.PortalBelgeNo) ? row.YerelFaturaNo : row.PortalBelgeNo;
            var tip = NormalizeInvoiceType(row.FaturaTipi) == "Alis" ? "Odeme" : "Tahsilat";

            return new TahsilatOdemeListeSatirDto
            {
                id = -row.Id,
                no = string.IsNullOrWhiteSpace(invoiceNo) ? $"Fatura #{row.Id}" : invoiceNo,
                tarih = row.Tarih.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                tip = tip,
                cariKartId = row.CariKartId,
                cariUnvan = string.IsNullOrWhiteSpace(cariName) ? $"Cari #{row.CariKartId}" : cariName.Trim(),
                odemeYontemi = ToUiPaymentLabel(row.OdemeYontemi),
                tutar = Math.Max(0m, row.GenelToplam - row.OdenenTutar),
                durum = "Bekliyor",
                kaynak = "Fatura",
                aciklama = string.IsNullOrWhiteSpace(row.Aciklama) ? "Bekleyen fatura" : row.Aciklama.Trim()
            };
        }

        private static string? BuildPaymentNote(TahsilatOdemeKaydetIstek request)
        {
            var parts = new List<string>();
            var payment = ToUiPaymentLabel(request.odemeYontemi);
            if (!string.IsNullOrWhiteSpace(payment))
                parts.Add(payment);

            if (!string.IsNullOrWhiteSpace(request.referansNo))
                parts.Add($"Ref: {request.referansNo.Trim()}");

            if (!string.IsNullOrWhiteSpace(request.kategori))
                parts.Add($"Kategori: {request.kategori.Trim()}");

            if (!string.IsNullOrWhiteSpace(request.aciklama))
                parts.Add(request.aciklama.Trim());

            if (!string.IsNullOrWhiteSpace(request.hizliNot))
                parts.Add(request.hizliNot.Trim());

            return parts.Count == 0 ? null : string.Join(" | ", parts);
        }

        private static string ExtractPaymentMethod(string? note)
        {
            var firstPart = (note ?? string.Empty).Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return string.IsNullOrWhiteSpace(firstPart) ? "Nakit" : firstPart.Trim();
        }

        private static string ExtractVisiblePaymentNote(string? note)
        {
            var parts = (note ?? string.Empty)
                .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !x.StartsWith("Ref:", StringComparison.OrdinalIgnoreCase) &&
                            !x.StartsWith("Kategori:", StringComparison.OrdinalIgnoreCase) &&
                            NormalizeAscii(x) is not ("nakit" or "kredikarti" or "kredi karti" or "onlineodeme" or "online odeme" or "havale"))
                .ToList();

            return parts.Count == 0 ? string.Empty : string.Join(" | ", parts);
        }

        private static StokHareketDto ToStockMovementDto(StokHareket row, IReadOnlyDictionary<int, string> productNames)
        {
            productNames.TryGetValue(row.UrunHizmetId, out var productName);
            return new StokHareketDto
            {
                id = row.Id,
                urunHizmetId = row.UrunHizmetId,
                urunAdi = string.IsNullOrWhiteSpace(productName) ? $"Ürün #{row.UrunHizmetId}" : productName.Trim(),
                tarih = row.Tarih.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                hareketTipi = row.Miktar >= 0 ? "Giris" : "Cikis",
                miktar = row.Miktar,
                kaynak = (row.Kaynak ?? string.Empty).Trim(),
                aciklama = (row.Aciklama ?? string.Empty).Trim()
            };
        }

        private static OzetKartDto ToOzetDto(string etiket, PeriodSummary summary)
        {
            return new OzetKartDto
            {
                etiket = etiket,
                aralik = FormatRange(summary.From, summary.To),
                gelir = summary.IncomeTotal,
                gider = summary.ExpenseTotal,
                net = summary.Net,
                gelirAdet = summary.IncomeCount,
                giderAdet = summary.ExpenseCount
            };
        }

        private static KarsilastirmaDto BuildDeltaDto(decimal current, decimal previous, bool positiveIsGood)
        {
            decimal delta;
            if (previous == 0m)
                delta = current == 0m ? 0m : 100m;
            else
                delta = Math.Round(((current - previous) / previous) * 100m, 0);

            var positive = delta >= 0m;
            return new KarsilastirmaDto
            {
                yuzde = delta,
                etiket = $"{(positive ? "+" : string.Empty)}{delta:0}%",
                olumlu = positiveIsGood ? positive : !positive
            };
        }

        private static OdemeDagilimDto ToOdemeDagilimDto(DailyPaymentMethodBreakdown row)
        {
            return new OdemeDagilimDto
            {
                yontem = ToUiPaymentLabel(row.Method),
                gelir = row.IncomeTotal,
                gider = row.ExpenseTotal,
                net = row.Net,
                toplam = Math.Abs(row.IncomeTotal) + Math.Abs(row.ExpenseTotal)
            };
        }

        private static List<NetTrendNoktaDto> BuildNetTrend(IReadOnlyCollection<Kasa> rows, DateTime today, int dayCount)
        {
            var result = new List<NetTrendNoktaDto>(dayCount);
            for (var offset = dayCount - 1; offset >= 0; offset--)
            {
                var date = today.AddDays(-offset).Date;
                var dayRows = rows
                    .Where(x => x.Tarih.Date == date)
                    .ToList();
                var income = dayRows
                    .Where(x => IsIncomeTip(x.Tip))
                    .Sum(x => x.Tutar);
                var expense = dayRows
                    .Where(x => IsExpenseTip(x.Tip))
                    .Sum(x => x.Tutar);

                result.Add(new NetTrendNoktaDto
                {
                    gun = date.ToString("dd.MM", CultureInfo.InvariantCulture),
                    net = income - expense,
                    islemVar = dayRows.Count > 0
                });
            }

            return result;
        }

        private static string FormatRange(DateTime from, DateTime to)
        {
            if (from.Date == to.Date)
                return from.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

            if (from.Year == to.Year)
                return $"{from:dd.MM} - {to:dd.MM.yyyy}";

            return $"{from:dd.MM.yyyy} - {to:dd.MM.yyyy}";
        }

        private static string BuildTelegramSummaryText(
            string title,
            DateTime from,
            DateTime to,
            PeriodSummary summary,
            IReadOnlyCollection<Kasa> records,
            string businessName)
        {
            var sb = new StringBuilder();
            sb.AppendLine(title);
            sb.AppendLine(AppLocalization.F("main.telegram.range", from, to));
            sb.AppendLine(AppLocalization.F("main.telegram.business", businessName));
            sb.AppendLine("--------------------------------");
            sb.AppendLine(AppLocalization.F("main.summary.income", summary.IncomeTotal));
            sb.AppendLine(AppLocalization.F("main.summary.expense", summary.ExpenseTotal));
            sb.AppendLine(AppLocalization.F("main.summary.net", summary.Net));
            sb.AppendLine(AppLocalization.F(
                "main.telegram.tx",
                summary.IncomeCount + summary.ExpenseCount,
                summary.IncomeCount,
                summary.ExpenseCount));
            AppendOdemeYontemiBreakdown(sb, records);
            AppendKalemBreakdown(sb, records, "Gelir");
            AppendKalemBreakdown(sb, records, "Gider");
            return sb.ToString().Trim();
        }

        private static void AppendKalemBreakdown(StringBuilder sb, IReadOnlyCollection<Kasa> records, string tip)
        {
            var kalemRows = records
                .Where(x => IsTip(x.Tip, tip))
                .GroupBy(GetKalemName, StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    Kalem = g.Key,
                    Toplam = g.Sum(x => x.Tutar),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Toplam)
                .ThenBy(x => x.Kalem, StringComparer.OrdinalIgnoreCase)
                .ToList();

            sb.AppendLine();
            sb.AppendLine(AppLocalization.F("main.telegram.categoryHeader", AppLocalization.GetTipDisplay(tip)));
            if (kalemRows.Count == 0)
            {
                sb.AppendLine(AppLocalization.T("main.telegram.noRecord"));
                return;
            }

            foreach (var row in kalemRows)
                sb.AppendLine(AppLocalization.F("main.telegram.categoryRow", row.Kalem, row.Toplam, row.Count));
        }

        private static void AppendOdemeYontemiBreakdown(StringBuilder sb, IReadOnlyCollection<Kasa> records)
        {
            var byMethod = records
                .GroupBy(x => NormalizeOdemeYontemi(x.OdemeYontemi), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Income = g.Where(x => IsTip(x.Tip, "Gelir")).Sum(x => x.Tutar),
                        Expense = g.Where(x => IsTip(x.Tip, "Gider")).Sum(x => x.Tutar)
                    },
                    StringComparer.OrdinalIgnoreCase);

            sb.AppendLine();
            sb.AppendLine(AppLocalization.T("main.telegram.methodsHeader"));
            foreach (var method in new[] { "Nakit", "KrediKarti", "OnlineOdeme", "Havale" })
            {
                var income = byMethod.TryGetValue(method, out var values) ? values.Income : 0m;
                var expense = byMethod.TryGetValue(method, out values) ? values.Expense : 0m;
                sb.AppendLine(AppLocalization.F("main.telegram.methodRow", GetOdemeYontemiLabel(method), income, expense, income - expense));
            }
        }

        private static bool IsTip(string? rawTip, string tip)
        {
            var normalized = (rawTip ?? string.Empty).Trim().ToLowerInvariant();
            if (tip == "Gelir")
                return normalized is "gelir" or "giris" or "giriş" or "income" or "einnahme";

            if (tip == "Gider")
                return normalized is "gider" or "cikis" or "çıkış" or "expense" or "ausgabe";

            return false;
        }

        private static string GetKalemName(Kasa row)
        {
            if (!string.IsNullOrWhiteSpace(row.Kalem))
                return row.Kalem.Trim();

            if (!string.IsNullOrWhiteSpace(row.GiderTuru))
                return row.GiderTuru.Trim();

            return IsTip(row.Tip, "Gider")
                ? AppLocalization.T("main.telegram.defaultExpenseCategory")
                : AppLocalization.T("main.telegram.defaultIncomeCategory");
        }

        private static string GetOdemeYontemiLabel(string method)
        {
            return method switch
            {
                "KrediKarti" => AppLocalization.T("payment.card"),
                "OnlineOdeme" => AppLocalization.T("payment.online"),
                "Havale" => AppLocalization.T("payment.transfer"),
                "Nakit" => AppLocalization.T("payment.cash"),
                _ => method
            };
        }

        private static string NormalizeOdemeYontemi(string? value)
        {
            var normalized = NormalizeAscii(value);
            return normalized switch
            {
                "nakit" => "Nakit",
                "cash" => "Nakit",
                "kredikarti" => "KrediKarti",
                "kredi karti" => "KrediKarti",
                "kart" => "KrediKarti",
                "creditcard" => "KrediKarti",
                "credit card" => "KrediKarti",
                "online" => "OnlineOdeme",
                "onlineodeme" => "OnlineOdeme",
                "online odeme" => "OnlineOdeme",
                "online payment" => "OnlineOdeme",
                "havale" => "Havale",
                "transfer" => "Havale",
                "bank transfer" => "Havale",
                _ => "Nakit"
            };
        }

        private static string ToUiPaymentLabel(string? value)
        {
            return NormalizeAscii(value) switch
            {
                "kredikarti" or "kredi karti" or "kart" => "Kredi Kartı",
                "onlineodeme" or "online odeme" or "online" => "Online Ödeme",
                "havale" or "transfer" => "Havale",
                _ => "Nakit"
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

        private static bool IsIncomeTip(string? tip)
        {
            var normalized = NormalizeAscii(tip);
            return normalized is "gelir" or "giris" or "income";
        }

        private static bool IsExpenseTip(string? tip)
        {
            var normalized = NormalizeAscii(tip);
            return normalized is "gider" or "cikis" or "expense";
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

        private static string NormalizeLanguageCode(string? value)
        {
            return NormalizeAscii(value) switch
            {
                "en" or "english" => "en",
                "de" or "deutsch" => "de",
                _ => "tr"
            };
        }

        private static string ToApiTur(string? value)
        {
            return NormalizeAscii(value) is "gider" or "cikis" or "expense"
                ? "gider"
                : "gelir";
        }

        private static string NormalizeCariTip(string? value)
        {
            return NormalizeAscii(value) switch
            {
                "musteri" => "Musteri",
                "tedarikci" => "Tedarikci",
                "herikisi" or "her ikisi" or "ikisi" => "HerIkisi",
                _ => string.Empty
            };
        }

        private static string ToCariTipLabel(string? value)
        {
            return NormalizeCariTip(value) switch
            {
                "Musteri" => "Müşteri",
                "Tedarikci" => "Tedarikçi",
                "HerIkisi" => "Her İkisi",
                _ => "Müşteri"
            };
        }

        private static string NormalizeCariMovementType(string? value)
        {
            return NormalizeAscii(value) switch
            {
                "borc" => "Borc",
                "alacak" => "Alacak",
                "tahsilat" => "Tahsilat",
                "odeme" => "Odeme",
                _ => string.Empty
            };
        }

        private static string NormalizeProductType(string? value)
        {
            return NormalizeAscii(value) switch
            {
                "hizmet" or "service" => "Hizmet",
                _ => "Urun"
            };
        }

        private static string NormalizeInvoiceType(string? value)
        {
            return NormalizeAscii(value) switch
            {
                "alis" or "alim" or "purchase" => "Alis",
                _ => "Satis"
            };
        }

        private static string ToCariMovementLabel(string? value)
        {
            return NormalizeCariMovementType(value) switch
            {
                "Borc" => "Borç",
                "Alacak" => "Alacak",
                "Tahsilat" => "Tahsilat",
                "Odeme" => "Ödeme",
                _ => "Borç"
            };
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

        public sealed record ApiHata(string mesaj);
        public sealed record ApiMesaj(string mesaj);
        public sealed record KimlikliApiMesaj(string mesaj, int id);
        public sealed record OdemeYontemiDto(string deger, string etiket);
        public sealed record StokUrunDto(int id, string ad, string birim);
        public sealed record SecenekDto(string deger, string etiket);
        public sealed record RaporSecimDto(string deger, string etiket, bool secili);
        public sealed record GibPortalTestSonucDto(bool basarili, string mesaj);
        public sealed record GibSmsBaslatDto(string mesaj, string operationId);

        public sealed class KolayKurulumEkranDto
        {
            public bool tamamlandi { get; set; }
            public int isletmeId { get; set; }
            public string isletmeAdi { get; set; } = string.Empty;
            public string hesapTipi { get; set; } = "Isletme";
            public string isletmeTuru { get; set; } = "Genel";
            public string konum { get; set; } = string.Empty;
            public bool muhasebeciVarMi { get; set; }
            public string mesaj { get; set; } = string.Empty;
            public List<KolayKurulumTurDto> turler { get; set; } = new();
        }

        public sealed class KolayKurulumTurDto
        {
            public string kod { get; set; } = string.Empty;
            public string ad { get; set; } = string.Empty;
            public string aciklama { get; set; } = string.Empty;
            public List<string> gelirKalemleri { get; set; } = new();
            public List<string> giderKalemleri { get; set; } = new();
        }

        public sealed class KolayKurulumKaydetIstek
        {
            public string? isletmeAdi { get; set; }
            public string? hesapTipi { get; set; }
            public string? isletmeTuru { get; set; }
            public string? konum { get; set; }
            public bool? muhasebeciVarMi { get; set; }
            public MuhasebeciProfilKaydetRequest? muhasebeciProfil { get; set; }
        }

        public sealed class TelegramEkranDto
        {
            public bool bagli { get; set; }
            public string durum { get; set; } = string.Empty;
            public string botKullaniciAdi { get; set; } = string.Empty;
            public string eslestirmeKodu { get; set; } = string.Empty;
            public string baglantiLinki { get; set; } = string.Empty;
            public string qrUrl { get; set; } = string.Empty;
            public int gecerlilikDakika { get; set; }
            public string mesaj { get; set; } = string.Empty;
        }

        public sealed class AyarlarEkranDto
        {
            public int aktifIsletmeId { get; set; }
            public string aktifIsletme { get; set; } = string.Empty;
            public int seciliIsletmeId { get; set; }
            public int? seciliKalemId { get; set; }
            public string dil { get; set; } = "tr";
            public List<AyarDilDto> diller { get; set; } = new();
            public List<AyarIsletmeDto> isletmeler { get; set; } = new();
            public List<AyarKalemDto> kalemler { get; set; } = new();
            public string mesaj { get; set; } = string.Empty;
        }

        public sealed class AyarDilDto
        {
            public string kod { get; set; } = string.Empty;
            public string ad { get; set; } = string.Empty;
        }

        public sealed class AyarIsletmeDto
        {
            public int id { get; set; }
            public string ad { get; set; } = string.Empty;
            public bool aktif { get; set; }
        }

        public sealed class AyarKalemDto
        {
            public int id { get; set; }
            public string tip { get; set; } = "Gelir";
            public string ad { get; set; } = string.Empty;
        }

        public sealed class AyarDilKaydetIstek
        {
            public string? dil { get; set; }
        }

        public sealed class AyarIsletmeKaydetIstek
        {
            public string? ad { get; set; }
        }

        public sealed class AyarKalemKaydetIstek
        {
            public string? tip { get; set; }
            public string? ad { get; set; }
        }

        public sealed class GibPortalEkranDto
        {
            public string aktifIsletme { get; set; } = string.Empty;
            public string kullaniciKodu { get; set; } = string.Empty;
            public bool hasPassword { get; set; }
            public bool testModu { get; set; }
            public string mesaj { get; set; } = string.Empty;
        }

        public sealed class GibPortalAyarKaydetIstek
        {
            public string? kullaniciKodu { get; set; }
            public string? sifre { get; set; }
            public bool testModu { get; set; }
        }

        public sealed class GibSmsTamamlaIstek
        {
            public string? operationId { get; set; }
            public string? smsKodu { get; set; }
        }

        public sealed class RaporlarEkranDto
        {
            public string aktifIsletme { get; set; } = string.Empty;
            public string bugun { get; set; } = string.Empty;
            public string varsayilanDonem { get; set; } = string.Empty;
            public string varsayilanKlasor { get; set; } = string.Empty;
            public List<RaporSecimDto> formatlar { get; set; } = new();
            public List<RaporSecimDto> icerikler { get; set; } = new();
            public List<SecenekDto> yazdirmaSablonlari { get; set; } = new();
            public List<SecenekDto> tarihAraliklari { get; set; } = new();
            public RaporPaketDto? sonPaket { get; set; }
        }

        public sealed class RaporPaketDto
        {
            public bool varMi { get; set; }
            public string ad { get; set; } = string.Empty;
            public string yol { get; set; } = string.Empty;
            public string klasor { get; set; } = string.Empty;
            public string donem { get; set; } = string.Empty;
            public string olusturmaZamani { get; set; } = string.Empty;
        }

        public sealed class RaporPaketOlusturIstek
        {
            public string? donem { get; set; }
            public string? klasor { get; set; }
            public List<string> formatlar { get; set; } = new();
            public List<string> icerikler { get; set; } = new();
        }

        public sealed class RaporYolIstek
        {
            public string? yol { get; set; }
        }

        public sealed record RaporKlasorDto(string yol);

        public sealed class RaporYazdirIstek
        {
            public string? sablon { get; set; }
            public string? aralikKodu { get; set; }
            public string? baslangic { get; set; }
            public string? bitis { get; set; }
            public string? notMetni { get; set; }
        }

        public sealed class DashboardEkranDto
        {
            public string aktifIsletme { get; set; } = string.Empty;
            public OzetKartDto bugun { get; set; } = new();
            public List<OzetKartDto> paneller { get; set; } = new();
            public KarsilastirmaDto gelirDegisim { get; set; } = new();
            public KarsilastirmaDto giderDegisim { get; set; } = new();
            public List<OdemeDagilimDto> odemeDagilimi { get; set; } = new();
            public List<NetTrendNoktaDto> netTrend { get; set; } = new();
            public MuhasebeciSohbetBildirimDurumuDto sohbet { get; set; } = new();
        }

        public sealed class UstBarDto
        {
            public int aktifIsletmeId { get; set; }
            public string aktifIsletme { get; set; } = string.Empty;
            public string hesapTipi { get; set; } = "Isletme";
            public bool muhasebeciMusteriBaglami { get; set; }
            public int? muhasebeciIsletmeId { get; set; }
            public string muhasebeciAdi { get; set; } = string.Empty;
            public string muhasebeciYetkiSeviyesi { get; set; } = "TamIslem";
            public bool telegramAktif { get; set; }
            public bool bildirimVar { get; set; }
            public int bildirimSayisi { get; set; }
            public MuhasebeciSohbetBildirimDurumuDto sohbet { get; set; } = new();
            public bool yoneticiMi { get; set; }
            public List<IsletmeSecenekDto> isletmeler { get; set; } = new();
        }

        public sealed class BildirimDto
        {
            public string id { get; set; } = string.Empty;
            public string tur { get; set; } = string.Empty;
            public string onem { get; set; } = "orta";
            public string baslik { get; set; } = string.Empty;
            public string mesaj { get; set; } = string.Empty;
            public string aksiyon { get; set; } = string.Empty;
            public string url { get; set; } = string.Empty;
        }

        public sealed class IsletmeSecenekDto
        {
            public int id { get; set; }
            public string ad { get; set; } = string.Empty;
            public bool aktif { get; set; }
        }

        public sealed class OzetKartDto
        {
            public string etiket { get; set; } = string.Empty;
            public string aralik { get; set; } = string.Empty;
            public decimal gelir { get; set; }
            public decimal gider { get; set; }
            public decimal net { get; set; }
            public int gelirAdet { get; set; }
            public int giderAdet { get; set; }
        }

        public sealed class KarsilastirmaDto
        {
            public decimal yuzde { get; set; }
            public string etiket { get; set; } = string.Empty;
            public bool olumlu { get; set; }
        }

        public sealed class OdemeDagilimDto
        {
            public string yontem { get; set; } = string.Empty;
            public decimal gelir { get; set; }
            public decimal gider { get; set; }
            public decimal net { get; set; }
            public decimal toplam { get; set; }
        }

        public sealed class NetTrendNoktaDto
        {
            public string gun { get; set; } = string.Empty;
            public decimal net { get; set; }
            public bool islemVar { get; set; }
        }

        public sealed class GelirGiderEkranDto
        {
            public string aktifIsletme { get; set; } = string.Empty;
            public List<KayitDto> kayitlar { get; set; } = new();
            public List<string> gelirKalemleri { get; set; } = new();
            public List<string> giderKalemleri { get; set; } = new();
            public List<StokUrunDto> stokUrunleri { get; set; } = new();
            public List<OdemeYontemiDto> odemeYontemleri { get; set; } = new();
        }

        public sealed class CariEkranDto
        {
            public string aktifIsletme { get; set; } = string.Empty;
            public List<CariListeSatirDto> kartlar { get; set; } = new();
            public List<SecenekDto> tipSecenekleri { get; set; } = new();
            public List<SecenekDto> hareketTipleri { get; set; } = new();
        }

        public sealed class UrunStokEkranDto
        {
            public string aktifIsletme { get; set; } = string.Empty;
            public List<UrunListeSatirDto> urunler { get; set; } = new();
            public List<StokHareketDto> sonHareketler { get; set; } = new();
            public List<SecenekDto> tipSecenekleri { get; set; } = new();
            public List<SecenekDto> birimSecenekleri { get; set; } = new();
        }

        public sealed class FaturaEkranDto
        {
            public string aktifIsletme { get; set; } = string.Empty;
            public List<FaturaListeSatirDto> faturalar { get; set; } = new();
            public List<FaturaCariSecenekDto> cariler { get; set; } = new();
            public List<FaturaUrunSecenekDto> urunler { get; set; } = new();
            public FaturaOzetDto ozet { get; set; } = new();
            public List<SecenekDto> faturaTipleri { get; set; } = new();
            public List<SecenekDto> odemeYontemleri { get; set; } = new();
            public string bugun { get; set; } = string.Empty;
        }

        public sealed class TahsilatOdemeEkranDto
        {
            public string aktifIsletme { get; set; } = string.Empty;
            public List<TahsilatOdemeListeSatirDto> hareketler { get; set; } = new();
            public List<FaturaCariSecenekDto> cariler { get; set; } = new();
            public List<TahsilatOdemeFaturaSecenekDto> faturalar { get; set; } = new();
            public TahsilatOdemeOzetDto ozet { get; set; } = new();
            public List<SecenekDto> islemTipleri { get; set; } = new();
            public List<SecenekDto> odemeYontemleri { get; set; } = new();
            public List<SecenekDto> paraBirimleri { get; set; } = new();
            public List<SecenekDto> kategoriler { get; set; } = new();
            public string bugun { get; set; } = string.Empty;
        }

        public sealed class TahsilatOdemeOzetDto
        {
            public decimal toplamTahsilat { get; set; }
            public int tahsilatAdedi { get; set; }
            public decimal toplamOdeme { get; set; }
            public int odemeAdedi { get; set; }
            public decimal bekleyen { get; set; }
            public int bekleyenAdedi { get; set; }
        }

        public sealed class TahsilatOdemeListeSatirDto
        {
            public int id { get; set; }
            public string no { get; set; } = string.Empty;
            public string tarih { get; set; } = string.Empty;
            public string tip { get; set; } = string.Empty;
            public int cariKartId { get; set; }
            public string cariUnvan { get; set; } = string.Empty;
            public string odemeYontemi { get; set; } = string.Empty;
            public decimal tutar { get; set; }
            public string durum { get; set; } = string.Empty;
            public string kaynak { get; set; } = string.Empty;
            public string aciklama { get; set; } = string.Empty;
        }

        public sealed class TahsilatOdemeFaturaSecenekDto
        {
            public int id { get; set; }
            public string no { get; set; } = string.Empty;
            public int cariKartId { get; set; }
            public string cariUnvan { get; set; } = string.Empty;
            public string faturaTipi { get; set; } = string.Empty;
            public string durum { get; set; } = string.Empty;
            public decimal genelToplam { get; set; }
            public decimal odenenTutar { get; set; }
            public decimal kalan { get; set; }
            public string odemeYontemi { get; set; } = string.Empty;
            public string aciklama { get; set; } = string.Empty;
        }

        public sealed class FaturaOzetDto
        {
            public decimal toplamFatura { get; set; }
            public int faturaAdedi { get; set; }
            public decimal tahsilEdilen { get; set; }
            public decimal bekleyen { get; set; }
            public int bekleyenAdedi { get; set; }
        }

        public sealed class FaturaCariSecenekDto
        {
            public int id { get; set; }
            public string unvan { get; set; } = string.Empty;
        }

        public sealed class FaturaUrunSecenekDto
        {
            public int id { get; set; }
            public string ad { get; set; } = string.Empty;
            public string tip { get; set; } = string.Empty;
            public string birim { get; set; } = string.Empty;
            public decimal kdvOrani { get; set; }
            public decimal alisFiyati { get; set; }
            public decimal satisFiyati { get; set; }
        }

        public sealed class FaturaListeSatirDto
        {
            public int id { get; set; }
            public string no { get; set; } = string.Empty;
            public string tarih { get; set; } = string.Empty;
            public string vadeTarihi { get; set; } = string.Empty;
            public string faturaTipi { get; set; } = string.Empty;
            public string durum { get; set; } = string.Empty;
            public int cariKartId { get; set; }
            public string cariUnvan { get; set; } = string.Empty;
            public decimal genelToplam { get; set; }
            public decimal odenenTutar { get; set; }
            public string odemeYontemi { get; set; } = string.Empty;
            public string aciklama { get; set; } = string.Empty;
        }

        public sealed class FaturaDetayDto
        {
            public FaturaFormDto fatura { get; set; } = new();
            public List<FaturaSatirDto> satirlar { get; set; } = new();
        }

        public sealed class FaturaFormDto
        {
            public int id { get; set; }
            public int cariKartId { get; set; }
            public string tarih { get; set; } = string.Empty;
            public string vadeTarihi { get; set; } = string.Empty;
            public string faturaTipi { get; set; } = string.Empty;
            public string durum { get; set; } = string.Empty;
            public string yerelFaturaNo { get; set; } = string.Empty;
            public string portalBelgeNo { get; set; } = string.Empty;
            public string portalUuid { get; set; } = string.Empty;
            public decimal araToplam { get; set; }
            public decimal iskontoToplam { get; set; }
            public decimal kdvToplam { get; set; }
            public decimal genelToplam { get; set; }
            public decimal odenenTutar { get; set; }
            public string odemeYontemi { get; set; } = string.Empty;
            public string aciklama { get; set; } = string.Empty;
            public string cariUnvan { get; set; } = string.Empty;
        }

        public sealed class FaturaSatirDto
        {
            public int id { get; set; }
            public int urunHizmetId { get; set; }
            public string aciklama { get; set; } = string.Empty;
            public string birim { get; set; } = string.Empty;
            public decimal miktar { get; set; }
            public decimal birimFiyat { get; set; }
            public decimal iskontoOrani { get; set; }
            public decimal kdvOrani { get; set; }
            public bool stokEtkilesin { get; set; }
            public decimal satirToplam { get; set; }
        }

        public sealed class UrunListeSatirDto
        {
            public int id { get; set; }
            public string tip { get; set; } = string.Empty;
            public string ad { get; set; } = string.Empty;
            public string barkod { get; set; } = string.Empty;
            public string birim { get; set; } = string.Empty;
            public decimal kdvOrani { get; set; }
            public decimal alisFiyati { get; set; }
            public decimal satisFiyati { get; set; }
            public decimal kritikStok { get; set; }
            public decimal mevcutStok { get; set; }
            public bool aktif { get; set; }
        }

        public sealed class StokHareketDto
        {
            public int id { get; set; }
            public int urunHizmetId { get; set; }
            public string urunAdi { get; set; } = string.Empty;
            public string tarih { get; set; } = string.Empty;
            public string hareketTipi { get; set; } = string.Empty;
            public decimal miktar { get; set; }
            public string kaynak { get; set; } = string.Empty;
            public string aciklama { get; set; } = string.Empty;
        }

        public sealed class StokHareketSonucDto
        {
            public string mesaj { get; set; } = string.Empty;
            public decimal mevcutStok { get; set; }
        }

        public sealed class CariListeSatirDto
        {
            public int id { get; set; }
            public string tip { get; set; } = string.Empty;
            public string unvan { get; set; } = string.Empty;
            public string telefon { get; set; } = string.Empty;
            public string vergiNo { get; set; } = string.Empty;
            public bool aktif { get; set; }
        }

        public sealed class CariDetayDto
        {
            public CariKartFormDto kart { get; set; } = new();
            public decimal bakiye { get; set; }
            public List<CariHareketDto> hareketler { get; set; } = new();
        }

        public sealed class CariKartFormDto
        {
            public int id { get; set; }
            public string tip { get; set; } = string.Empty;
            public string unvan { get; set; } = string.Empty;
            public string telefon { get; set; } = string.Empty;
            public string eposta { get; set; } = string.Empty;
            public string vergiNoTc { get; set; } = string.Empty;
            public string vergiDairesi { get; set; } = string.Empty;
            public string adres { get; set; } = string.Empty;
            public bool aktif { get; set; }
        }

        public sealed class CariHareketDto
        {
            public int id { get; set; }
            public string tarih { get; set; } = string.Empty;
            public string hareketTipi { get; set; } = string.Empty;
            public string kaynak { get; set; } = string.Empty;
            public string aciklama { get; set; } = string.Empty;
            public decimal tutar { get; set; }
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

        public sealed class CariKaydetIstek
        {
            public int? id { get; set; }
            public string? tip { get; set; }
            public string? unvan { get; set; }
            public string? telefon { get; set; }
            public string? eposta { get; set; }
            public string? vergiNoTc { get; set; }
            public string? vergiDairesi { get; set; }
            public string? adres { get; set; }
            public bool aktif { get; set; } = true;
        }

        public sealed class CariHareketKaydetIstek
        {
            public string? hareketTipi { get; set; }
            public decimal tutar { get; set; }
            public string? tarih { get; set; }
            public string? aciklama { get; set; }
        }

        public sealed class UrunHizmetKaydetIstek
        {
            public int? id { get; set; }
            public string? tip { get; set; }
            public string? ad { get; set; }
            public string? barkod { get; set; }
            public string? birim { get; set; }
            public decimal kdvOrani { get; set; }
            public decimal alisFiyati { get; set; }
            public decimal satisFiyati { get; set; }
            public decimal kritikStok { get; set; }
            public bool aktif { get; set; } = true;
        }

        public sealed class StokHareketKaydetIstek
        {
            public decimal miktar { get; set; }
            public string? tarih { get; set; }
            public string? aciklama { get; set; }
        }

        public sealed class FaturaKaydetIstek
        {
            public int? id { get; set; }
            public int cariKartId { get; set; }
            public string? tarih { get; set; }
            public string? vadeTarihi { get; set; }
            public string? faturaTipi { get; set; }
            public string? odemeYontemi { get; set; }
            public string? aciklama { get; set; }
            public List<FaturaSatirKaydetIstek> satirlar { get; set; } = new();
        }

        public sealed class FaturaSatirKaydetIstek
        {
            public int urunHizmetId { get; set; }
            public string? aciklama { get; set; }
            public string? birim { get; set; }
            public decimal miktar { get; set; }
            public decimal birimFiyat { get; set; }
            public decimal iskontoOrani { get; set; }
            public decimal kdvOrani { get; set; }
            public bool stokEtkilesin { get; set; } = true;
        }

        public sealed class FaturaTahsilatKaydetIstek
        {
            public decimal tutar { get; set; }
            public string? tarih { get; set; }
            public string? odemeYontemi { get; set; }
            public string? aciklama { get; set; }
        }

        public sealed class TahsilatOdemeKaydetIstek
        {
            public string? islemTipi { get; set; }
            public int cariKartId { get; set; }
            public string? tarih { get; set; }
            public string? odemeYontemi { get; set; }
            public string? vadeTarihi { get; set; }
            public string? aciklama { get; set; }
            public decimal tutar { get; set; }
            public string? paraBirimi { get; set; }
            public string? referansNo { get; set; }
            public string? kategori { get; set; }
            public int faturaId { get; set; }
            public bool faturaIleEslestir { get; set; }
            public string? hizliNot { get; set; }
        }

        public sealed class PinDogrulaIstek
        {
            public string? pin { get; set; }
        }

        public sealed class StokGirisIstek
        {
            public bool aktif { get; set; }
            public int urunId { get; set; }
            public decimal miktar { get; set; }
        }
    }
}
