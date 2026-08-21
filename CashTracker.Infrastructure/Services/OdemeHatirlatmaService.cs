using System.Net.Mail;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services;

public sealed class OdemeHatirlatmaService : IOdemeHatirlatmaService
{
    private const string Gonderildi = "Gonderildi";
    private const string Basarisiz = "Basarisiz";
    private static readonly TimeSpan TekrarBeklemeSuresi = TimeSpan.FromHours(24);

    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
    private readonly IIsletmeService _isletmeService;
    private readonly IOdemeHatirlatmaSender _sender;

    public OdemeHatirlatmaService(
        IDbContextFactory<CashTrackerDbContext> dbFactory,
        IIsletmeService isletmeService,
        IOdemeHatirlatmaSender sender)
    {
        _dbFactory = dbFactory;
        _isletmeService = isletmeService;
        _sender = sender;
    }

    public async Task<OdemeHatirlatmaOnizleme> GetPreviewAsync(int faturaId, CancellationToken ct = default)
    {
        var state = await LoadStateAsync(faturaId, ct);
        var validation = Validate(state);
        var content = BuildContent(state);

        return new OdemeHatirlatmaOnizleme(
            state.Fatura.Id,
            state.IsletmeAdi,
            state.Cari.Eposta.Trim(),
            DisplayName(state.Cari.Unvan, $"Cari #{state.Cari.Id}"),
            InvoiceNo(state.Fatura),
            state.Fatura.Tarih,
            state.Fatura.VadeTarihi,
            Math.Max(0m, state.Fatura.GenelToplam - state.Fatura.OdenenTutar),
            "TRY",
            OdemeHatirlatmaMetni.BuildSubject(content),
            state.Fatura.VadeTarihi.HasValue ? OdemeHatirlatmaMetni.BuildMessage(content) : string.Empty,
            validation.Length == 0,
            validation,
            state.SonGonderimAt);
    }

    public async Task<OdemeHatirlatmaGonderimSonucu> SendAsync(int faturaId, CancellationToken ct = default)
    {
        var state = await LoadStateAsync(faturaId, ct);
        var validation = Validate(state);
        if (validation.Length > 0)
            return new OdemeHatirlatmaGonderimSonucu(false, validation, null);

        var content = BuildContent(state);
        var sent = await _sender.SendAsync(content, ct);
        var now = DateTime.Now;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.OdemeHatirlatmalari.Add(new OdemeHatirlatma
        {
            IsletmeId = state.Fatura.IsletmeId,
            FaturaId = state.Fatura.Id,
            CariKartId = state.Cari.Id,
            AliciEposta = state.Cari.Eposta.Trim(),
            Konu = OdemeHatirlatmaMetni.BuildSubject(content),
            Durum = sent ? Gonderildi : Basarisiz,
            Hata = sent ? string.Empty : "E-posta sağlayıcısı gönderimi tamamlayamadı.",
            GonderildiAt = sent ? now : null,
            CreatedAt = now
        });
        await db.SaveChangesAsync(ct);

        return sent
            ? new OdemeHatirlatmaGonderimSonucu(true, $"Hatırlatma {state.Cari.Eposta.Trim()} adresine gönderildi.", now)
            : new OdemeHatirlatmaGonderimSonucu(false, "Hatırlatma gönderilemedi. E-posta ayarlarını kontrol edin.", null);
    }

    private async Task<ReminderState> LoadStateAsync(int faturaId, CancellationToken ct)
    {
        if (faturaId <= 0)
            throw new ArgumentException("Geçerli bir fatura seçin.", nameof(faturaId));

        var active = await _isletmeService.GetActiveAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var fatura = await db.Faturalar.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == faturaId && x.IsletmeId == active.Id, ct)
            ?? throw new InvalidOperationException("Fatura bulunamadı.");
        var cari = await db.CariKartlari.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == fatura.CariKartId && x.IsletmeId == active.Id, ct)
            ?? throw new InvalidOperationException("Faturanın cari kaydı bulunamadı.");
        var sonGonderim = await db.OdemeHatirlatmalari.AsNoTracking()
            .Where(x => x.IsletmeId == active.Id && x.FaturaId == fatura.Id && x.Durum == Gonderildi)
            .OrderByDescending(x => x.GonderildiAt)
            .Select(x => x.GonderildiAt)
            .FirstOrDefaultAsync(ct);

        return new ReminderState(fatura, cari, DisplayName(active.Ad, "İşletme"), sonGonderim);
    }

    private string Validate(ReminderState state)
    {
        if (!string.Equals(state.Fatura.FaturaTipi, "Satis", StringComparison.OrdinalIgnoreCase))
            return "Yalnızca satış faturaları için hatırlatma gönderebilirsiniz.";
        if (state.Fatura.Durum is not (FaturaDurum.Kesildi or FaturaDurum.KismiOdendi))
            return "Fatura kesilmeden hatırlatma gönderemezsiniz.";
        if (state.Fatura.GenelToplam - state.Fatura.OdenenTutar <= 0)
            return "Bu fatura tamamen ödenmiş.";
        if (!state.Fatura.VadeTarihi.HasValue)
            return "Hatırlatma göndermek için faturaya vade tarihi ekleyin.";
        if (!MailAddress.TryCreate(state.Cari.Eposta.Trim(), out _))
            return "Hatırlatma göndermek için cari karta geçerli bir e-posta ekleyin.";
        if (!_sender.IsConfigured)
            return "Systemcel e-posta gönderimi henüz yapılandırılmamış.";
        if (state.SonGonderimAt.HasValue && DateTime.Now - state.SonGonderimAt.Value < TekrarBeklemeSuresi)
            return "Bu faturanın hatırlatması son 24 saat içinde gönderildi.";
        return string.Empty;
    }

    private static OdemeHatirlatmaIcerigi BuildContent(ReminderState state)
    {
        return new OdemeHatirlatmaIcerigi(
            state.Fatura.IsletmeId,
            state.IsletmeAdi,
            state.Cari.Eposta.Trim(),
            DisplayName(state.Cari.Unvan, $"Cari #{state.Cari.Id}"),
            InvoiceNo(state.Fatura),
            state.Fatura.Tarih,
            state.Fatura.VadeTarihi ?? state.Fatura.Tarih,
            Math.Max(0m, state.Fatura.GenelToplam - state.Fatura.OdenenTutar),
            "TRY");
    }

    private static string InvoiceNo(Fatura fatura)
    {
        var value = string.IsNullOrWhiteSpace(fatura.PortalBelgeNo) ? fatura.YerelFaturaNo : fatura.PortalBelgeNo;
        return DisplayName(value, $"Fatura #{fatura.Id}");
    }

    private static string DisplayName(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private sealed record ReminderState(
        Fatura Fatura,
        CariKart Cari,
        string IsletmeAdi,
        DateTime? SonGonderimAt);
}
