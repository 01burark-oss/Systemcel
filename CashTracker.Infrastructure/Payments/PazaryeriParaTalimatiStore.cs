using System.Data;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Payments;

public sealed record PazaryeriParaTalimatiTaslagi(
    int AliciIsletmeId,
    int? TedarikciSiparisId,
    int? PazaryeriOdemeId,
    string Tur,
    string Saglayici,
    string IdempotencyAnahtari,
    string KaynakRef,
    decimal Tutar,
    string ParaBirimi);

/// <summary>
/// Persists payment instructions inside the caller's business transaction.
/// A claimed instruction is never sent again automatically after an unknown result.
/// </summary>
public sealed class PazaryeriParaTalimatiStore(IDbContextFactory<CashTrackerDbContext> dbFactory)
{
    public static async Task<PazaryeriParaTalimati> EnqueueAsync(
        CashTrackerDbContext db,
        PazaryeriParaTalimatiTaslagi draft,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(draft);
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Payment instruction requires the caller's business transaction.");
        ValidateDraft(draft);
        var existing = db.PazaryeriParaTalimatlari.Local.SingleOrDefault(x =>
            x.Saglayici == draft.Saglayici && x.Tur == draft.Tur &&
            x.IdempotencyAnahtari == draft.IdempotencyAnahtari)
            ?? await db.PazaryeriParaTalimatlari.SingleOrDefaultAsync(x =>
                x.Saglayici == draft.Saglayici && x.Tur == draft.Tur &&
                x.IdempotencyAnahtari == draft.IdempotencyAnahtari, ct);
        if (existing is not null)
        {
            if (existing.AliciIsletmeId != draft.AliciIsletmeId ||
                existing.TedarikciSiparisId != draft.TedarikciSiparisId ||
                existing.PazaryeriOdemeId != draft.PazaryeriOdemeId ||
                existing.KaynakRef != draft.KaynakRef || existing.Tutar != draft.Tutar ||
                existing.ParaBirimi != draft.ParaBirimi)
                throw new InvalidOperationException("Payment instruction key was used with different data.");
            return existing;
        }

        var now = DateTime.UtcNow;
        var instruction = new PazaryeriParaTalimati
        {
            AliciIsletmeId = draft.AliciIsletmeId,
            TedarikciSiparisId = draft.TedarikciSiparisId,
            PazaryeriOdemeId = draft.PazaryeriOdemeId,
            Tur = draft.Tur,
            Saglayici = draft.Saglayici,
            IdempotencyAnahtari = draft.IdempotencyAnahtari,
            KaynakRef = draft.KaynakRef,
            Tutar = draft.Tutar,
            ParaBirimi = draft.ParaBirimi,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.PazaryeriParaTalimatlari.Add(instruction);
        return instruction;
    }

    public async Task<PazaryeriParaTalimati?> ClaimReadyAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("UTC time required.", nameof(nowUtc));
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var instructionId = await ReadyForDispatch(db)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
            .Select(x => (long?)x.Id).FirstOrDefaultAsync(ct);
        if (instructionId is not { } id || !await ClaimByIdAsync(id, nowUtc, ct)) return null;
        return await db.PazaryeriParaTalimatlari.AsNoTracking().SingleAsync(x => x.Id == id, ct);
    }

    public async Task<bool> ClaimByIdAsync(long instructionId, DateTime nowUtc, CancellationToken ct = default)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("UTC time required.", nameof(nowUtc));
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var instruction = await ReadyForDispatch(db).SingleOrDefaultAsync(x => x.Id == instructionId, ct);
        if (instruction is null) return false;
        instruction.Durum = PazaryeriParaTalimatiDurumlari.Gonderiliyor;
        instruction.DenemeSayisi++;
        instruction.GonderimBasladiAt = nowUtc;
        instruction.UpdatedAt = nowUtc;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    internal static IQueryable<PazaryeriParaTalimati> ReadyForDispatch(CashTrackerDbContext db) =>
        db.PazaryeriParaTalimatlari.Where(instruction =>
            instruction.Durum == PazaryeriParaTalimatiDurumlari.Hazir &&
            (instruction.Tur == PazaryeriParaTalimatiTurleri.Tahsilat ||
             !db.TedarikciMalKabulDuzeltmeleri.Any(x =>
                 (x.TedarikciSiparisId == instruction.TedarikciSiparisId ||
                  (instruction.Tur == PazaryeriParaTalimatiTurleri.Iade && db.PazaryeriOdemeDagitimlari.Any(a => a.PazaryeriOdemeId == instruction.PazaryeriOdemeId && a.TedarikciSiparisId == x.TedarikciSiparisId))) &&
                 (x.Durum == "OnayBekliyor" || (x.Durum == "Uygulandi" && ((x.ParaDurumu != "Uzlasti" && x.ParaDurumu != "Uygulanmaz") || x.BelgeDurumu != "Tamamlandi" || x.StokDurumu != "Tamamlandi"))))) &&
            (instruction.Tur != PazaryeriParaTalimatiTurleri.Aktarim ||
             (!db.TedarikciSiparisleri.Any(x => x.Id == instruction.TedarikciSiparisId &&
                 (x.Durum == PazaryeriSiparisDurumlari.Itirazli ||
                  x.Durum == PazaryeriSiparisDurumlari.IptalEdildi ||
                  x.Durum == PazaryeriSiparisDurumlari.IadeEdildi)) &&
              !db.TedarikciMalKabulleri.Any(x => x.TedarikciSiparisId == instruction.TedarikciSiparisId && x.OnayDurumu == "OnayBekliyor") &&
              !db.TedarikciSiparisSikayetleri.Any(x => x.TedarikciSiparisId == instruction.TedarikciSiparisId &&
                  (x.Durum == TedarikciSikayetDurumlari.Acik || x.Durum == TedarikciSikayetDurumlari.Yanitlandi || x.Durum == TedarikciSikayetDurumlari.Cozulemedi)) &&
              !db.PazaryeriParaTalimatlari.Any(x =>
                  x.Id != instruction.Id && x.TedarikciSiparisId == instruction.TedarikciSiparisId &&
                  x.Tur == PazaryeriParaTalimatiTurleri.Aktarim &&
                  (x.Durum == PazaryeriParaTalimatiDurumlari.Gonderiliyor ||
                   x.Durum == PazaryeriParaTalimatiDurumlari.SonucBekliyor ||
                   x.Durum == PazaryeriParaTalimatiDurumlari.IncelemeGerekli)) &&
              !db.PazaryeriParaTalimatlari.Any(x =>
                  x.PazaryeriOdemeId == instruction.PazaryeriOdemeId &&
                  x.Tur == PazaryeriParaTalimatiTurleri.Iade &&
                  x.Durum != PazaryeriParaTalimatiDurumlari.Tamamlandi))) &&
            (instruction.Tur != PazaryeriParaTalimatiTurleri.Iade ||
             !db.PazaryeriParaTalimatlari.Any(x =>
                 x.Id != instruction.Id && x.PazaryeriOdemeId == instruction.PazaryeriOdemeId &&
                 (x.Durum == PazaryeriParaTalimatiDurumlari.Gonderiliyor ||
                  x.Durum == PazaryeriParaTalimatiDurumlari.SonucBekliyor ||
                  x.Durum == PazaryeriParaTalimatiDurumlari.IncelemeGerekli))));

    public async Task MarkUnknownAsync(long instructionId, string errorCode, DateTime nowUtc, CancellationToken ct = default)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("UTC time required.", nameof(nowUtc));
        if (string.IsNullOrWhiteSpace(errorCode) || errorCode.Length > 80)
            throw new ArgumentException("A short error code is required.", nameof(errorCode));
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var instruction = await db.PazaryeriParaTalimatlari.SingleAsync(x => x.Id == instructionId, ct);
        if (instruction.Durum == PazaryeriParaTalimatiDurumlari.SonucBekliyor &&
            instruction.SonHataKodu == errorCode) return;
        if (instruction.Durum != PazaryeriParaTalimatiDurumlari.Gonderiliyor)
            throw new InvalidOperationException("Only a claimed instruction can enter unknown-result review.");
        instruction.Durum = PazaryeriParaTalimatiDurumlari.SonucBekliyor;
        instruction.SonHataKodu = errorCode;
        instruction.UpdatedAt = nowUtc;
        if (instruction.Tur is PazaryeriParaTalimatiTurleri.Aktarim or PazaryeriParaTalimatiTurleri.Iade)
        {
            var settlements = await db.TedarikciHakEdisleri.Where(x =>
                instruction.TedarikciSiparisId != null
                    ? x.TedarikciSiparisId == instruction.TedarikciSiparisId
                    : db.TedarikciSiparisleri.Any(o => o.Id == x.TedarikciSiparisId &&
                        db.PazaryeriOdemeleri.Any(p => p.Id == instruction.PazaryeriOdemeId && p.AnaSiparisId == o.AnaSiparisId)))
                .ToListAsync(ct);
            foreach (var settlement in settlements)
            {
                settlement.Durum = instruction.Tur == PazaryeriParaTalimatiTurleri.Iade
                    ? "IadeBekliyor" : "MutabakatFarki";
                settlement.UpdatedAt = nowUtc;
            }
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public static async Task ApplyCompletedAsync(
        CashTrackerDbContext db,
        long instructionId,
        string providerTransactionId,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Payment completion requires the caller's ledger transaction.");
        if (nowUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("UTC time required.", nameof(nowUtc));
        if (string.IsNullOrWhiteSpace(providerTransactionId) || providerTransactionId.Length > 180)
            throw new ArgumentException("Provider transaction ID is required.", nameof(providerTransactionId));
        var instruction = await db.PazaryeriParaTalimatlari.SingleAsync(x => x.Id == instructionId, ct);
        if (instruction.Durum == PazaryeriParaTalimatiDurumlari.Tamamlandi &&
            instruction.SaglayiciIslemId == providerTransactionId) return;
        if (instruction.Durum is not (PazaryeriParaTalimatiDurumlari.Gonderiliyor or PazaryeriParaTalimatiDurumlari.SonucBekliyor))
            throw new InvalidOperationException("Instruction is not awaiting a provider result.");
        instruction.Durum = PazaryeriParaTalimatiDurumlari.Tamamlandi;
        instruction.SaglayiciIslemId = providerTransactionId;
        instruction.SonHataKodu = string.Empty;
        instruction.SonuclandiAt = nowUtc;
        instruction.UpdatedAt = nowUtc;
    }

    public static async Task ApplyDefinitiveFailureAsync(
        CashTrackerDbContext db,
        long instructionId,
        string errorCode,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Payment failure requires the caller's business transaction.");
        if (nowUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("UTC time required.", nameof(nowUtc));
        if (string.IsNullOrWhiteSpace(errorCode) || errorCode.Length > 80)
            throw new ArgumentException("A short error code is required.", nameof(errorCode));
        var instruction = await db.PazaryeriParaTalimatlari.SingleAsync(x => x.Id == instructionId, ct);
        if (instruction.Durum != PazaryeriParaTalimatiDurumlari.Gonderiliyor)
            throw new InvalidOperationException("Only a claimed instruction can fail definitively.");
        instruction.Durum = PazaryeriParaTalimatiDurumlari.KesinBasarisiz;
        instruction.SonHataKodu = errorCode;
        instruction.SonuclandiAt = nowUtc;
        instruction.UpdatedAt = nowUtc;
    }

    private static void ValidateDraft(PazaryeriParaTalimatiTaslagi draft)
    {
        if (draft.AliciIsletmeId <= 0 || draft.Tutar <= 0m || decimal.Round(draft.Tutar, 2) != draft.Tutar)
            throw new ArgumentException("Instruction business and amount must be valid.", nameof(draft));
        if (draft.Tur is not (PazaryeriParaTalimatiTurleri.Tahsilat or PazaryeriParaTalimatiTurleri.Iade or PazaryeriParaTalimatiTurleri.Aktarim))
            throw new ArgumentException("Instruction type is invalid.", nameof(draft));
        if (string.IsNullOrWhiteSpace(draft.Saglayici) || draft.Saglayici.Length > 80 ||
            string.IsNullOrWhiteSpace(draft.IdempotencyAnahtari) || draft.IdempotencyAnahtari.Length > 100 ||
            string.IsNullOrWhiteSpace(draft.KaynakRef) || draft.KaynakRef.Length > 160 ||
            draft.ParaBirimi is not ("TRY" or "EUR" or "USD" or "GBP" or "RUB"))
            throw new ArgumentException("Instruction reference or currency is invalid.", nameof(draft));
    }
}
