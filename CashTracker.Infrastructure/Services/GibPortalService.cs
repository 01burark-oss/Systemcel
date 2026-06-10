using System;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services
{
    public sealed class GibPortalService : IGibPortalService
    {
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;
        private readonly ISecretProtector _secretProtector;
        private readonly IGibPortalClient _client;
        private readonly IFaturaService _faturaService;

        public GibPortalService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService,
            ISecretProtector secretProtector,
            IGibPortalClient client,
            IFaturaService faturaService)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
            _secretProtector = secretProtector;
            _client = client;
            _faturaService = faturaService;
        }

        public async Task<GibPortalSettingsModel?> GetSettingsAsync(CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var row = await db.GibPortalAyarlari.AsNoTracking().FirstOrDefaultAsync(x => x.IsletmeId == activeIsletmeId, ct);
            if (row == null)
                return null;

            return new GibPortalSettingsModel
            {
                KullaniciKodu = row.KullaniciKodu,
                HasPassword = !string.IsNullOrWhiteSpace(row.SifreCipherText),
                TestModu = row.TestModu
            };
        }

        public async Task SaveSettingsAsync(GibPortalSaveSettingsRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.KullaniciKodu))
                throw new ArgumentException("GİB kullanıcı kodu boş olamaz.", nameof(request));

            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var row = await db.GibPortalAyarlari.FirstOrDefaultAsync(x => x.IsletmeId == activeIsletmeId, ct);
            var now = DateTime.Now;
            var cipher = string.IsNullOrWhiteSpace(request.Sifre)
                ? row?.SifreCipherText ?? string.Empty
                : _secretProtector.Protect(request.Sifre);

            if (row == null)
            {
                row = new GibPortalAyar
                {
                    IsletmeId = activeIsletmeId,
                    CreatedAt = now
                };
                db.GibPortalAyarlari.Add(row);
            }

            row.KullaniciKodu = request.KullaniciKodu.Trim();
            row.SifreCipherText = cipher;
            row.TestModu = request.TestModu;
            row.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
        }

        public async Task<GibPortalResult> TestConnectionAsync(CancellationToken ct = default)
        {
            var settings = await LoadSettingsWithSecretAsync(ct);
            if (settings == null)
                return GibPortalResult.Fail("GİB Portal ayarları eksik.");

            var result = await _client.TestLoginAsync(settings.KullaniciKodu, settings.Sifre, settings.TestModu, ct);
            await LogAsync(null, "TestConnection", result, ct);
            return result;
        }

        public async Task<GibPortalResult> CreatePortalDraftAsync(int faturaId, CancellationToken ct = default)
        {
            var settings = await LoadSettingsWithSecretAsync(ct);
            if (settings == null)
                return GibPortalResult.Fail("GİB Portal ayarları eksik veya şifre çözümlenemedi.");

            var detail = await _faturaService.GetDetailAsync(faturaId, ct);
            if (detail == null)
                return GibPortalResult.Fail("Fatura bulunamadı.");

            var result = await _client.CreateDraftAsync(detail, settings.KullaniciKodu, settings.Sifre, settings.TestModu, ct);
            if (result.Success)
                await _faturaService.MarkAsPortalDraftAsync(faturaId, result.Uuid, result.BelgeNo, ct);

            await LogAsync(faturaId, "CreatePortalDraft", result, ct);
            return result;
        }

        public async Task<GibPortalResult> StartSmsApprovalAsync(int faturaId, CancellationToken ct = default)
        {
            var settings = await LoadSettingsWithSecretAsync(ct);
            if (settings == null)
                return GibPortalResult.Fail("GİB Portal ayarları eksik veya şifre çözümlenemedi.");

            var detail = await _faturaService.GetDetailAsync(faturaId, ct);
            if (detail == null)
                return GibPortalResult.Fail("Fatura bulunamadı.");

            if (string.IsNullOrWhiteSpace(detail.Fatura.PortalUuid))
                return GibPortalResult.Fail("Önce GİB Portal taslağı oluşturulmalı.");

            var result = await _client.StartSmsVerificationAsync(
                detail.Fatura.PortalUuid,
                settings.KullaniciKodu,
                settings.Sifre,
                settings.TestModu,
                ct);
            await LogAsync(faturaId, "StartSmsApproval", result, ct);
            return result;
        }

        public async Task<GibPortalResult> CompleteSmsApprovalAsync(int faturaId, string operationId, string smsCode, CancellationToken ct = default)
        {
            var settings = await LoadSettingsWithSecretAsync(ct);
            if (settings == null)
                return GibPortalResult.Fail("GİB Portal ayarları eksik veya şifre çözümlenemedi.");

            var detail = await _faturaService.GetDetailAsync(faturaId, ct);
            if (detail == null)
                return GibPortalResult.Fail("Fatura bulunamadı.");

            if (string.IsNullOrWhiteSpace(detail.Fatura.PortalUuid))
                return GibPortalResult.Fail("Portal UUID bulunamadı.");

            var result = await _client.CompleteSmsVerificationAsync(
                detail.Fatura.PortalUuid,
                operationId,
                smsCode,
                settings.KullaniciKodu,
                settings.Sifre,
                settings.TestModu,
                ct);
            if (result.Success)
                await _faturaService.MarkAsIssuedAsync(faturaId, ct);

            await LogAsync(faturaId, "CompleteSmsApproval", result, ct);
            return result;
        }

        private async Task<ResolvedSettings?> LoadSettingsWithSecretAsync(CancellationToken ct)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var row = await db.GibPortalAyarlari.AsNoTracking().FirstOrDefaultAsync(x => x.IsletmeId == activeIsletmeId, ct);
            if (row == null || string.IsNullOrWhiteSpace(row.KullaniciKodu))
                return null;

            if (!_secretProtector.TryUnprotect(row.SifreCipherText, out var password))
                return null;

            return new ResolvedSettings(row.KullaniciKodu, password, row.TestModu);
        }

        private async Task LogAsync(int? faturaId, string islem, GibPortalResult result, CancellationToken ct)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.GibPortalIslemLoglari.Add(new GibPortalIslemLog
            {
                IsletmeId = activeIsletmeId,
                FaturaId = faturaId,
                Tarih = DateTime.Now,
                Islem = islem,
                Basarili = result.Success,
                Mesaj = Sanitize(result.Message)
            });
            await db.SaveChangesAsync(ct);
        }

        private static string Sanitize(string message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? string.Empty
                : message.Trim();
        }

        private sealed record ResolvedSettings(string KullaniciKodu, string Sifre, bool TestModu);
    }
}
