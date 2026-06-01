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
    public sealed class AiUsageQuotaService : IAiUsageQuotaService
    {
        private const int UnlimitedWindowLimit = 30;
        private const int UnlimitedWindowHours = 4;

        private readonly IIsletmeService _isletmeService;
        private readonly ISubscriptionEntitlementService _entitlementService;
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;

        public AiUsageQuotaService(
            IIsletmeService isletmeService,
            ISubscriptionEntitlementService entitlementService,
            IDbContextFactory<CashTrackerDbContext> dbFactory)
        {
            _isletmeService = isletmeService;
            _entitlementService = entitlementService;
            _dbFactory = dbFactory;
        }

        public Task<AiUsageStatus> GetStatusAsync(CancellationToken ct = default)
        {
            return ResolveAsync(consume: false, ct);
        }

        public Task<AiUsageStatus> ConsumeAsync(CancellationToken ct = default)
        {
            return ResolveAsync(consume: true, ct);
        }

        private async Task<AiUsageStatus> ResolveAsync(bool consume, CancellationToken ct)
        {
            var now = DateTime.Now;
            var isletme = await _isletmeService.GetActiveAsync();
            var entitlement = await _entitlementService.GetIsletmeEntitlementAsync(isletme.Id, now, ct);

            var period = BuildPeriod(entitlement, now);
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var usage = await db.AiKullanimDonemleri
                .FirstOrDefaultAsync(x => x.IsletmeId == isletme.Id && x.DonemAnahtari == period.Key, ct);

            if (usage is null)
            {
                usage = new AiKullanimDonemi
                {
                    IsletmeId = isletme.Id,
                    DonemAnahtari = period.Key,
                    MesajLimiti = period.Limit,
                    DonemBaslangicAt = period.Start,
                    DonemBitisAt = period.End,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.AiKullanimDonemleri.Add(usage);
            }
            else
            {
                usage.MesajLimiti = period.Limit;
                usage.DonemBaslangicAt = period.Start;
                usage.DonemBitisAt = period.End;
                usage.UpdatedAt = now;
            }

            var allowed = entitlement.AiAktif && (period.Limit is null || usage.KullanilanMesaj < period.Limit.Value);
            if (consume && allowed)
            {
                usage.KullanilanMesaj++;
                usage.UpdatedAt = now;
            }

            await db.SaveChangesAsync(ct);

            return BuildStatus(entitlement, usage, period, consume && !allowed);
        }

        private static AiUsagePeriod BuildPeriod(SubscriptionEntitlementStatus entitlement, DateTime now)
        {
            if (entitlement.AiSinirsiz)
            {
                var windowHour = now.Hour / UnlimitedWindowHours * UnlimitedWindowHours;
                var start = new DateTime(now.Year, now.Month, now.Day, windowHour, 0, 0);
                return new AiUsagePeriod(
                    $"ai:4saat:{start:yyyyMMddHH}",
                    "4 saatlik",
                    UnlimitedWindowLimit,
                    start,
                    start.AddHours(UnlimitedWindowHours));
            }

            var monthStart = new DateTime(now.Year, now.Month, 1);
            return new AiUsagePeriod(
                $"ai:ay:{monthStart:yyyyMM}",
                "Aylık",
                entitlement.AiMesajLimiti ?? 0,
                monthStart,
                monthStart.AddMonths(1));
        }

        private static AiUsageStatus BuildStatus(
            SubscriptionEntitlementStatus entitlement,
            AiKullanimDonemi usage,
            AiUsagePeriod period,
            bool limitExceeded)
        {
            var kalan = period.Limit.HasValue
                ? Math.Max(0, period.Limit.Value - usage.KullanilanMesaj)
                : (int?)null;

            var message = !entitlement.AiAktif
                ? "Yapay zeka özellikleri için ücretli plana geçiş yapın."
                : limitExceeded
                    ? $"AI kullanım limitiniz doldu. {period.DonemTipi.ToLowerInvariant()} limit {period.End:HH:mm dd.MM.yyyy} tarihinde yenilenir."
                    : period.Limit.HasValue
                        ? $"{period.DonemTipi} AI hakkı: {kalan}/{period.Limit.Value}."
                        : "AI hakkı sınırsız.";

            return new AiUsageStatus
            {
                AiAktif = entitlement.AiAktif,
                PlanKodu = entitlement.PlanKodu,
                PlanAdi = entitlement.PlanAdi,
                SinirsizPlan = entitlement.AiSinirsiz,
                DonemTipi = period.DonemTipi,
                Limit = period.Limit,
                Kullanilan = usage.KullanilanMesaj,
                Kalan = kalan,
                DonemBitisAt = period.End,
                IzinVerildi = entitlement.AiAktif && !limitExceeded,
                LimitAsildi = limitExceeded,
                Mesaj = message
            };
        }

        private sealed record AiUsagePeriod(
            string Key,
            string DonemTipi,
            int? Limit,
            DateTime Start,
            DateTime End);
    }
}
