using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services
{
    public sealed class MuhasebeciPortalService : IMuhasebeciPortalService
    {
        private const string AuthProvider = "clerk";
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly IIsletmeService _isletmeService;
        private readonly ISubscriptionEntitlementService _entitlementService;

        public MuhasebeciPortalService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            ICurrentUserContext currentUserContext,
            IIsletmeService isletmeService,
            ISubscriptionEntitlementService entitlementService)
        {
            _dbFactory = dbFactory;
            _currentUserContext = currentUserContext;
            _isletmeService = isletmeService;
            _entitlementService = entitlementService;
        }

        public async Task<MuhasebeciPazaryeriDto> GetPublicMarketplaceAsync(string? arama = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return new MuhasebeciPazaryeriDto
            {
                Profiller = await BuildMarketplaceProfilesAsync(db, arama, viewerBusinessId: null, ct)
            };
        }

        public async Task<MuhasebeciPazaryeriDto> GetMarketplaceAsync(string? arama = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var active = await _isletmeService.GetActiveAsync();
            var isBusiness = string.Equals(active.TenantTipi, HesapTipleri.Isletme, StringComparison.OrdinalIgnoreCase);
            var isAccountant = string.Equals(active.TenantTipi, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase);
            if (!isBusiness && !isAccountant)
                throw new InvalidOperationException("Muhasebeci pazaryeri işletme ve muhasebeci hesapları içindir.");

            return new MuhasebeciPazaryeriDto
            {
                Profiller = await BuildMarketplaceProfilesAsync(db, arama, isBusiness ? active.Id : null, ct)
            };
        }

        public async Task<MuhasebeciPanelDto> GetPanelAsync(CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var user = await EnsureCurrentUserAsync(db, ct);
            var accountant = await FindCurrentAccountantBusinessAsync(db, user, ct);
            if (accountant == null)
            {
                var active = await _isletmeService.GetActiveAsync();
                return new MuhasebeciPanelDto
                {
                    Hazir = false,
                    Mesaj = string.Equals(active.TenantTipi, HesapTipleri.Isletme, StringComparison.OrdinalIgnoreCase)
                        ? "Muhasebeci paneli yalnızca muhasebeci hesapları içindir."
                        : "Muhasebeci paneli için ilk kurulumda hesap tipini Muhasebeci olarak seçin."
                };
            }

            if (RequiresAccountantApproval(user))
            {
                var rejected = string.Equals(user?.Durum, KullaniciDurumlari.MuhasebeciReddedildi, StringComparison.OrdinalIgnoreCase);
                return new MuhasebeciPanelDto
                {
                    Hazir = false,
                    MuhasebeciIsletmeId = accountant.Id,
                    MuhasebeciAdi = DisplayName(accountant.Ad, "Muhasebeci"),
                    Mesaj = rejected
                        ? "Muhasebeci başvurunuz reddedildi. Tekrar değerlendirme için kurulum bilgilerinizi güncelleyip yeniden başvuru gönderebilirsiniz."
                        : "Muhasebeci başvurunuz onay bekliyor. Onay tamamlanmadan panel, davet ve pazaryeri yayını açılmaz."
                };
            }

            var profile = await db.MuhasebeciProfilleri.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MuhasebeciIsletmeId == accountant.Id, ct);
            var entitlement = await _entitlementService.GetMuhasebeciEntitlementAsync(accountant.Id, ct: ct);
            var relations = await db.MuhasebeciMusterileri.AsNoTracking()
                .Where(x => x.MuhasebeciIsletmeId == accountant.Id && x.Durum == "Aktif")
                .ToListAsync(ct);
            var customerIds = relations.Select(x => x.MusteriIsletmeId).Distinct().ToList();
            var customers = await db.Isletmeler.AsNoTracking()
                .Where(x => customerIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);

            var pending = await db.MuhasebeciMusteriTalepleri.AsNoTracking()
                .Where(x => x.MuhasebeciIsletmeId == accountant.Id && x.Durum == MuhasebeciTalepDurumlari.Beklemede)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(ct);

            var pendingCustomerIds = pending.Where(x => x.MusteriIsletmeId.HasValue).Select(x => x.MusteriIsletmeId!.Value).Distinct().ToList();
            var pendingCustomers = await db.Isletmeler.AsNoTracking()
                .Where(x => pendingCustomerIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);

            return new MuhasebeciPanelDto
            {
                Hazir = true,
                MuhasebeciIsletmeId = accountant.Id,
                MuhasebeciAdi = DisplayName(accountant.Ad, "Muhasebeci"),
                Entitlement = entitlement,
                Profil = BuildProfileDto(accountant, profile, entitlement, talepVar: false, bagli: false),
                Musteriler = relations
                    .Select(x =>
                    {
                        customers.TryGetValue(x.MusteriIsletmeId, out var customer);
                        return new MuhasebeciMusteriDto
                        {
                            IsletmeId = x.MusteriIsletmeId,
                            Ad = DisplayName(customer?.Ad, "Musteri"),
                            Konum = customer?.Konum ?? string.Empty,
                            YetkiSeviyesi = NormalizeYetki(x.YetkiSeviyesi),
                            Durum = x.Durum,
                            BaslangicAt = x.BaslangicAt
                        };
                    })
                    .OrderBy(x => x.Ad)
                    .ToList(),
                BekleyenTalepler = pending
                    .Where(x => x.Tur == MuhasebeciTalepTurleri.Pazaryeri)
                    .Select(x => BuildTalepDto(x, accountant, x.MusteriIsletmeId.HasValue && pendingCustomers.TryGetValue(x.MusteriIsletmeId.Value, out var customer) ? customer : null))
                    .ToList(),
                Davetler = pending
                    .Where(x => x.Tur == MuhasebeciTalepTurleri.Davet)
                    .Select(x => BuildTalepDto(x, accountant, null))
                    .ToList()
            };
        }

        public async Task<MuhasebeciProfilDto> SaveProfileAsync(MuhasebeciProfilKaydetRequest request, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var user = await EnsureCurrentUserAsync(db, ct);
            var accountant = await RequireCurrentAccountantBusinessAsync(db, user, ct);
            RequireApprovedAccountantUser(user);
            var now = DateTime.Now;
            var profile = await db.MuhasebeciProfilleri.FirstOrDefaultAsync(x => x.MuhasebeciIsletmeId == accountant.Id, ct);

            if (profile == null)
            {
                profile = new MuhasebeciProfil
                {
                    MuhasebeciIsletmeId = accountant.Id,
                    CreatedAt = now
                };
                db.MuhasebeciProfilleri.Add(profile);
            }

            profile.Yayinda = request.Yayinda;
            profile.Unvan = NormalizeText(request.Unvan, accountant.Ad);
            profile.Konum = NormalizeText(request.Konum, accountant.Konum);
            profile.Telefon = request.Yayinda ? NormalizeRequiredText(request.Telefon, "Telefon numarası") : NormalizeText(request.Telefon, profile.Telefon);
            profile.DeneyimYili = Math.Max(0, request.DeneyimYili);
            profile.ProfilResmiUrl = request.Yayinda ? NormalizeRequiredText(request.ProfilResmiUrl, "Profil resmi") : NormalizeText(request.ProfilResmiUrl, profile.ProfilResmiUrl);
            profile.UcretBilgisi = request.Yayinda ? NormalizeRequiredText(request.UcretBilgisi, "Ücret bilgisi") : NormalizeText(request.UcretBilgisi, profile.UcretBilgisi);
            profile.Uzmanliklar = NormalizeText(request.Uzmanliklar, "Genel muhasebe");
            profile.MusteriTipleri = NormalizeText(request.MusteriTipleri, "KOBİ ve küçük işletmeler");
            profile.KisaAciklama = NormalizeText(request.KisaAciklama, "Gelir, gider, fatura ve dönem takibinde destek olur.");
            profile.UpdatedAt = now;
            await db.SaveChangesAsync(ct);

            var entitlement = await _entitlementService.GetMuhasebeciEntitlementAsync(accountant.Id, ct: ct);
            return BuildProfileDto(accountant, profile, entitlement, talepVar: false, bagli: false);
        }

        public async Task<MuhasebeciTalepDto> CreateInviteAsync(
            MuhasebeciTalepOlusturRequest request,
            string publicBaseUrl,
            CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var user = await EnsureCurrentUserAsync(db, ct);
            var accountant = await RequireCurrentAccountantBusinessAsync(db, user, ct);
            RequireApprovedAccountantUser(user);
            var now = DateTime.Now;
            var code = await GenerateInviteCodeAsync(db, ct);
            var talep = new MuhasebeciMusteriTalebi
            {
                MuhasebeciIsletmeId = accountant.Id,
                TalepEdenIsletmeId = accountant.Id,
                Tur = MuhasebeciTalepTurleri.Davet,
                Durum = MuhasebeciTalepDurumlari.Beklemede,
                YetkiSeviyesi = NormalizeYetki(request.YetkiSeviyesi),
                DavetKodu = code,
                Mesaj = NormalizeConversationText(request.Mesaj, allowEmpty: true),
                CreatedAt = now,
                UpdatedAt = now
            };

            db.MuhasebeciMusteriTalepleri.Add(talep);
            await db.SaveChangesAsync(ct);
            return BuildTalepDto(talep, accountant, null, publicBaseUrl);
        }

        public async Task<MuhasebeciTalepDto> SubmitMarketplaceRequestAsync(
            int muhasebeciIsletmeId,
            MuhasebeciTalepOlusturRequest request,
            CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await EnsureCurrentUserAsync(db, ct);
            var customer = await _isletmeService.GetActiveAsync();
            if (!string.Equals(customer.TenantTipi, HesapTipleri.Isletme, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Pazaryeri talebi için aktif hesap işletme olmalıdır.");

            var accountant = await db.Isletmeler.FirstOrDefaultAsync(x => x.Id == muhasebeciIsletmeId && x.TenantTipi == HesapTipleri.Muhasebeci, ct)
                ?? throw new InvalidOperationException("Muhasebeci bulunamadı.");
            var profilePublished = await db.MuhasebeciProfilleri.AnyAsync(x => x.MuhasebeciIsletmeId == muhasebeciIsletmeId && x.Yayinda, ct);
            if (!profilePublished)
                throw new InvalidOperationException("Bu muhasebeci pazaryerinde yayında değil.");

            await EnsureNoActiveOrPendingPairAsync(db, muhasebeciIsletmeId, customer.Id, ct);
            var now = DateTime.Now;
            var talep = new MuhasebeciMusteriTalebi
            {
                MuhasebeciIsletmeId = muhasebeciIsletmeId,
                MusteriIsletmeId = customer.Id,
                TalepEdenIsletmeId = customer.Id,
                Tur = MuhasebeciTalepTurleri.Pazaryeri,
                Durum = MuhasebeciTalepDurumlari.Beklemede,
                YetkiSeviyesi = NormalizeYetki(request.YetkiSeviyesi),
                Mesaj = NormalizeConversationText(request.Mesaj, allowEmpty: true),
                CreatedAt = now,
                UpdatedAt = now
            };

            db.MuhasebeciMusteriTalepleri.Add(talep);
            await db.SaveChangesAsync(ct);
            return BuildTalepDto(talep, accountant, customer);
        }

        public async Task<MuhasebeciTalepDto> AcceptInviteAsync(MuhasebeciDavetKabulRequest request, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await EnsureCurrentUserAsync(db, ct);
            var customer = await _isletmeService.GetActiveAsync();
            if (!string.Equals(customer.TenantTipi, HesapTipleri.Isletme, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Daveti kabul etmek için aktif hesap işletme olmalıdır.");

            var code = NormalizeInviteCode(request.DavetKodu);
            var talep = await db.MuhasebeciMusteriTalepleri.FirstOrDefaultAsync(x =>
                x.Tur == MuhasebeciTalepTurleri.Davet &&
                x.Durum == MuhasebeciTalepDurumlari.Beklemede &&
                x.DavetKodu == code, ct)
                ?? throw new InvalidOperationException("Davet bulunamadı veya artık aktif değil.");

            talep.MusteriIsletmeId = customer.Id;
            talep.YetkiSeviyesi = NormalizeYetki(request.YetkiSeviyesi);
            await AcceptTalepAsync(db, talep, customer.Id, talep.YetkiSeviyesi, ct);

            var accountant = await db.Isletmeler.AsNoTracking().FirstAsync(x => x.Id == talep.MuhasebeciIsletmeId, ct);
            return BuildTalepDto(talep, accountant, customer);
        }

        public async Task<MuhasebeciTalepDto> AcceptRequestAsync(int talepId, MuhasebeciTalepKararRequest request, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var user = await EnsureCurrentUserAsync(db, ct);
            var talep = await RequirePendingRequestAsync(db, talepId, ct);
            if (!talep.MusteriIsletmeId.HasValue)
                throw new InvalidOperationException("Talep henüz bir müşteriye bağlı değil.");

            if (user == null)
                await RequireActiveLegacyBusinessParticipantAsync(db, talep.MuhasebeciIsletmeId, ct);
            else
                await RequireAccountantParticipantAsync(db, user.Id, talep.MuhasebeciIsletmeId, ct);
            RequireApprovedAccountantUser(user);
            talep.YetkiSeviyesi = NormalizeYetki(request.YetkiSeviyesi);
            await AcceptTalepAsync(db, talep, talep.MusteriIsletmeId.Value, talep.YetkiSeviyesi, ct);

            return await BuildTalepDtoAsync(db, talep, ct);
        }

        public async Task<MuhasebeciTalepDto> RejectRequestAsync(int talepId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var user = await EnsureCurrentUserAsync(db, ct);
            var talep = await RequirePendingRequestAsync(db, talepId, ct);
            if (user == null)
                await RequireActiveLegacyRequestParticipantAsync(db, talep, ct);
            else
                await RequireRequestParticipantAsync(db, user.Id, talep, ct);
            talep.Durum = MuhasebeciTalepDurumlari.Red;
            talep.SonucAt = DateTime.Now;
            talep.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync(ct);
            return await BuildTalepDtoAsync(db, talep, ct);
        }

        public async Task<MuhasebeciTalepDto> CancelRequestAsync(int talepId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var user = await EnsureCurrentUserAsync(db, ct);
            var talep = await RequirePendingRequestAsync(db, talepId, ct);
            if (user == null)
                await RequireActiveLegacyRequestParticipantAsync(db, talep, ct);
            else
                await RequireRequestParticipantAsync(db, user.Id, talep, ct);
            talep.Durum = MuhasebeciTalepDurumlari.Iptal;
            talep.SonucAt = DateTime.Now;
            talep.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync(ct);
            return await BuildTalepDtoAsync(db, talep, ct);
        }

        public async Task<MuhasebeciSohbetDto> GetCustomerConversationAsync(int muhasebeciIsletmeId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await EnsureCurrentUserAsync(db, ct);
            var customer = await _isletmeService.GetActiveAsync();
            if (!string.Equals(customer.TenantTipi, HesapTipleri.Isletme, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Sohbet için aktif hesap işletme olmalıdır.");

            var conversation = await RequireCustomerConversationAsync(db, muhasebeciIsletmeId, customer.Id, ct);
            await MarkConversationReadAsync(db, conversation, customer.Id, ct);
            return await BuildConversationDtoAsync(db, conversation, customer.Id, ct);
        }

        public async Task<MuhasebeciSohbetDto> SendCustomerConversationMessageAsync(
            int muhasebeciIsletmeId,
            MuhasebeciSohbetMesajiGonderRequest request,
            CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await EnsureCurrentUserAsync(db, ct);
            var customer = await _isletmeService.GetActiveAsync();
            if (!string.Equals(customer.TenantTipi, HesapTipleri.Isletme, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Sohbet için aktif hesap işletme olmalıdır.");

            var conversation = await RequireCustomerConversationAsync(db, muhasebeciIsletmeId, customer.Id, ct);
            await AddConversationMessageAsync(db, conversation, customer.Id, request.Mesaj, ct);
            return await BuildConversationDtoAsync(db, conversation, customer.Id, ct);
        }

        public async Task<MuhasebeciSohbetDto> GetAccountantRequestConversationAsync(int talepId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var user = await EnsureCurrentUserAsync(db, ct);
            RequireApprovedAccountantUser(user);
            var talep = await RequireAccountantConversationRequestAsync(db, user!.Id, talepId, ct);
            var conversation = new ConversationContext(talep.MuhasebeciIsletmeId, talep.MusteriIsletmeId!.Value, talep.Id, null, "Talep bekliyor");
            await MarkConversationReadAsync(db, conversation, talep.MuhasebeciIsletmeId, ct);
            return await BuildConversationDtoAsync(db, conversation, talep.MuhasebeciIsletmeId, ct);
        }

        public async Task<MuhasebeciSohbetDto> SendAccountantRequestConversationMessageAsync(
            int talepId,
            MuhasebeciSohbetMesajiGonderRequest request,
            CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var user = await EnsureCurrentUserAsync(db, ct);
            RequireApprovedAccountantUser(user);
            var talep = await RequireAccountantConversationRequestAsync(db, user!.Id, talepId, ct);
            var conversation = new ConversationContext(talep.MuhasebeciIsletmeId, talep.MusteriIsletmeId!.Value, talep.Id, null, "Talep bekliyor");
            await AddConversationMessageAsync(db, conversation, talep.MuhasebeciIsletmeId, request.Mesaj, ct);
            return await BuildConversationDtoAsync(db, conversation, talep.MuhasebeciIsletmeId, ct);
        }

        public async Task<MuhasebeciSohbetDto> GetAccountantCustomerConversationAsync(int musteriIsletmeId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var user = await EnsureCurrentUserAsync(db, ct);
            var accountant = await RequireCurrentAccountantBusinessAsync(db, user, ct);
            RequireApprovedAccountantUser(user);
            var conversation = await RequireAccountantCustomerConversationAsync(db, accountant.Id, musteriIsletmeId, ct);
            await MarkConversationReadAsync(db, conversation, accountant.Id, ct);
            return await BuildConversationDtoAsync(db, conversation, accountant.Id, ct);
        }

        public async Task<MuhasebeciSohbetDto> SendAccountantCustomerConversationMessageAsync(
            int musteriIsletmeId,
            MuhasebeciSohbetMesajiGonderRequest request,
            CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var user = await EnsureCurrentUserAsync(db, ct);
            var accountant = await RequireCurrentAccountantBusinessAsync(db, user, ct);
            RequireApprovedAccountantUser(user);
            var conversation = await RequireAccountantCustomerConversationAsync(db, accountant.Id, musteriIsletmeId, ct);
            await AddConversationMessageAsync(db, conversation, accountant.Id, request.Mesaj, ct);
            return await BuildConversationDtoAsync(db, conversation, accountant.Id, ct);
        }

        public async Task<MuhasebeciSohbetBildirimDurumuDto> GetConversationNotificationStatusAsync(CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var active = await _isletmeService.GetActiveAsync();
            var access = await _isletmeService.GetActiveAccessAsync();
            var viewerBusinessId = access.MuhasebeciMusteriBaglami && access.MuhasebeciIsletmeId.HasValue
                ? access.MuhasebeciIsletmeId.Value
                : active.Id;

            var messages = await db.MuhasebeciSohbetMesajlari.AsNoTracking()
                .Where(x =>
                    x.MuhasebeciIsletmeId == viewerBusinessId || x.MusteriIsletmeId == viewerBusinessId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);

            if (messages.Count == 0)
                return new MuhasebeciSohbetBildirimDurumuDto();

            var businessIds = messages
                .SelectMany(x => new[] { x.MuhasebeciIsletmeId, x.MusteriIsletmeId })
                .Distinct()
                .ToList();
            var businesses = await db.Isletmeler.AsNoTracking()
                .Where(x => businessIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);

            var accountantIds = messages.Select(x => x.MuhasebeciIsletmeId).Distinct().ToList();
            var customerIds = messages.Select(x => x.MusteriIsletmeId).Distinct().ToList();
            var activeRelations = await db.MuhasebeciMusterileri.AsNoTracking()
                .Where(x =>
                    x.Durum == "Aktif" &&
                    accountantIds.Contains(x.MuhasebeciIsletmeId) &&
                    customerIds.Contains(x.MusteriIsletmeId))
                .ToListAsync(ct);
            var relationByPair = activeRelations
                .GroupBy(x => (x.MuhasebeciIsletmeId, x.MusteriIsletmeId))
                .ToDictionary(x => x.Key, x => x.OrderByDescending(relation => relation.Id).First());

            var pendingRequests = await db.MuhasebeciMusteriTalepleri.AsNoTracking()
                .Where(x =>
                    x.MusteriIsletmeId.HasValue &&
                    x.Durum == MuhasebeciTalepDurumlari.Beklemede &&
                    accountantIds.Contains(x.MuhasebeciIsletmeId) &&
                    customerIds.Contains(x.MusteriIsletmeId.Value))
                .ToListAsync(ct);
            var requestByPair = pendingRequests
                .GroupBy(x => (x.MuhasebeciIsletmeId, MusteriIsletmeId: x.MusteriIsletmeId!.Value))
                .ToDictionary(x => x.Key, x => x.OrderByDescending(request => request.CreatedAt).ThenByDescending(request => request.Id).First());

            var conversations = messages
                .GroupBy(x => new { x.MuhasebeciIsletmeId, x.MusteriIsletmeId })
                .Where(group =>
                    relationByPair.ContainsKey((group.Key.MuhasebeciIsletmeId, group.Key.MusteriIsletmeId)) ||
                    requestByPair.ContainsKey((group.Key.MuhasebeciIsletmeId, group.Key.MusteriIsletmeId)))
                .Select(group =>
                {
                    var last = group.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).First();
                    var pair = (last.MuhasebeciIsletmeId, last.MusteriIsletmeId);
                    relationByPair.TryGetValue(pair, out var relation);
                    requestByPair.TryGetValue(pair, out var request);
                    businesses.TryGetValue(last.MuhasebeciIsletmeId, out var accountant);
                    businesses.TryGetValue(last.MusteriIsletmeId, out var customer);
                    var title = last.MuhasebeciIsletmeId == viewerBusinessId
                        ? DisplayName(customer?.Ad, "Müşteri")
                        : DisplayName(accountant?.Ad, "Muhasebeci");
                    var talepId = relation?.TalepId ?? request?.Id ?? last.TalepId;
                    var baglantiId = relation?.Id ?? last.BaglantiId;

                    return new MuhasebeciSohbetBildirimDto
                    {
                        MuhasebeciIsletmeId = last.MuhasebeciIsletmeId,
                        MusteriIsletmeId = last.MusteriIsletmeId,
                        TalepId = talepId,
                        BaglantiId = baglantiId,
                        Baslik = title,
                        SonMesaj = last.Mesaj,
                        SonMesajAt = last.CreatedAt,
                        OkunmamisMesajSayisi = group.Count(x => x.OkunduAt == null && x.GonderenIsletmeId != viewerBusinessId),
                        HedefUrl = last.MuhasebeciIsletmeId == viewerBusinessId
                            ? baglantiId.HasValue
                                ? $"/app/muhasebeci?musteriId={last.MusteriIsletmeId}&sohbet=1"
                                : $"/app/muhasebeci?talepId={talepId ?? 0}&sohbet=1"
                            : $"/app/muhasebeciler?muhasebeciId={last.MuhasebeciIsletmeId}&talep=1"
                    };
                })
                .OrderByDescending(x => x.SonMesajAt)
                .Take(10)
                .ToList();

            return new MuhasebeciSohbetBildirimDurumuDto
            {
                OkunmamisMesajSayisi = conversations.Sum(x => x.OkunmamisMesajSayisi),
                Sohbetler = conversations
            };
        }

        public async Task OpenCustomerContextAsync(int musteriIsletmeId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var user = await EnsureCurrentUserAsync(db, ct);
            RequireApprovedAccountantUser(user);
            await _isletmeService.SetActiveCustomerContextAsync(musteriIsletmeId);
        }

        public Task CloseCustomerContextAsync(CancellationToken ct = default)
        {
            return _isletmeService.ClearActiveCustomerContextAsync();
        }

        private async Task<List<MuhasebeciProfilDto>> BuildMarketplaceProfilesAsync(
            CashTrackerDbContext db,
            string? arama,
            int? viewerBusinessId,
            CancellationToken ct)
        {
            var profiles = await db.MuhasebeciProfilleri.AsNoTracking()
                .Where(x => x.Yayinda)
                .ToListAsync(ct);
            var normalizedSearch = NormalizeSearch(arama);
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                profiles = profiles
                    .Where(x => NormalizeSearch($"{x.Unvan} {x.Konum} {x.UcretBilgisi} {x.Uzmanliklar} {x.MusteriTipleri} {x.KisaAciklama}").Contains(normalizedSearch))
                    .ToList();
            }

            var accountantIds = profiles.Select(x => x.MuhasebeciIsletmeId).Distinct().ToList();
            var accountants = await db.Isletmeler.AsNoTracking()
                .Where(x => accountantIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);
            var approvedAccountantIds = (await db.IsletmeUyelikleri.AsNoTracking()
                    .Where(x => accountantIds.Contains(x.IsletmeId) && x.Durum == "Aktif")
                    .Join(
                        db.Kullanicilar.AsNoTracking().Where(x =>
                            x.HesapTipi == HesapTipleri.Muhasebeci &&
                            x.Durum == KullaniciDurumlari.Aktif),
                        membership => membership.KullaniciId,
                        user => user.Id,
                        (membership, _) => membership.IsletmeId)
                    .Distinct()
                    .ToListAsync(ct))
                .ToHashSet();
            var proIds = await GetActiveProAccountantIdsAsync(db, accountantIds, ct);
            var pendingIds = new HashSet<int>();
            var connectedIds = new HashSet<int>();

            if (viewerBusinessId.HasValue)
            {
                pendingIds = (await db.MuhasebeciMusteriTalepleri.AsNoTracking()
                        .Where(x => x.MusteriIsletmeId == viewerBusinessId.Value && x.Durum == MuhasebeciTalepDurumlari.Beklemede)
                        .Select(x => x.MuhasebeciIsletmeId)
                        .ToListAsync(ct))
                    .ToHashSet();
                connectedIds = (await db.MuhasebeciMusterileri.AsNoTracking()
                        .Where(x => x.MusteriIsletmeId == viewerBusinessId.Value && x.Durum == "Aktif")
                        .Select(x => x.MuhasebeciIsletmeId)
                        .ToListAsync(ct))
                    .ToHashSet();
            }

            return profiles
                .Where(x => accountants.ContainsKey(x.MuhasebeciIsletmeId) && approvedAccountantIds.Contains(x.MuhasebeciIsletmeId))
                .Select(x =>
                {
                    var isPro = proIds.Contains(x.MuhasebeciIsletmeId);
                    return BuildProfileDto(
                        accountants[x.MuhasebeciIsletmeId],
                        x,
                        planAdi: string.Empty,
                        pro: isPro,
                        talepVar: pendingIds.Contains(x.MuhasebeciIsletmeId),
                        bagli: connectedIds.Contains(x.MuhasebeciIsletmeId),
                        telefonGoster: false);
                })
                .OrderByDescending(x => x.Pro)
                .ThenBy(x => x.Unvan)
                .ToList();
        }

        private async Task<Kullanici?> EnsureCurrentUserAsync(CashTrackerDbContext db, CancellationToken ct)
        {
            var identity = _currentUserContext.GetCurrentUser();
            if (identity == null)
                return null;

            var user = await db.Kullanicilar.FirstOrDefaultAsync(x =>
                x.AuthProvider == AuthProvider &&
                x.AuthProviderUserId == identity.ProviderUserId, ct);
            if (user != null)
                return user;

            await _isletmeService.GetActiveAsync();
            return await db.Kullanicilar.FirstOrDefaultAsync(x =>
                x.AuthProvider == AuthProvider &&
                x.AuthProviderUserId == identity.ProviderUserId, ct);
        }

        private async Task<Kullanici> RequireCurrentUserAsync(CashTrackerDbContext db, CancellationToken ct)
        {
            return await EnsureCurrentUserAsync(db, ct)
                ?? throw new InvalidOperationException("Bu işlem için oturum gerekir.");
        }

        private async Task<Isletme?> FindCurrentAccountantBusinessAsync(CashTrackerDbContext db, Kullanici? user, CancellationToken ct)
        {
            if (user != null)
                return await FindAccountantBusinessAsync(db, user.Id, ct);

            var active = await _isletmeService.GetActiveAsync();
            if (!string.Equals(active.TenantTipi, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase))
                return null;

            return await db.Isletmeler.AsNoTracking().FirstOrDefaultAsync(x => x.Id == active.Id, ct) ?? active;
        }

        private async Task<Isletme> RequireCurrentAccountantBusinessAsync(CashTrackerDbContext db, Kullanici? user, CancellationToken ct)
        {
            return await FindCurrentAccountantBusinessAsync(db, user, ct)
                ?? throw new InvalidOperationException("Muhasebeci çalışma alanı bulunamadı.");
        }

        private async Task<Isletme?> FindAccountantBusinessAsync(CashTrackerDbContext db, int userId, CancellationToken ct)
        {
            var access = await _isletmeService.GetActiveAccessAsync();
            if (!access.MuhasebeciMusteriBaglami)
            {
                var active = await db.Isletmeler.AsNoTracking().FirstOrDefaultAsync(x => x.Id == access.IsletmeId, ct);
                if (active != null && string.Equals(active.TenantTipi, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase))
                    return active;
            }

            return await db.IsletmeUyelikleri.AsNoTracking()
                .Where(x => x.KullaniciId == userId && x.Durum == "Aktif")
                .Join(
                    db.Isletmeler.AsNoTracking().Where(x => x.TenantTipi == HesapTipleri.Muhasebeci),
                    membership => membership.IsletmeId,
                    business => business.Id,
                    (membership, business) => business)
                .OrderBy(x => x.Ad)
                .FirstOrDefaultAsync(ct);
        }

        private async Task<Isletme> RequireAccountantBusinessAsync(CashTrackerDbContext db, int userId, CancellationToken ct)
        {
            return await FindAccountantBusinessAsync(db, userId, ct)
                ?? throw new InvalidOperationException("Muhasebeci çalışma alanı bulunamadı.");
        }

        private static bool IsApprovedAccountantUser(Kullanici user)
        {
            return string.Equals(user.HesapTipi, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(user.Durum, KullaniciDurumlari.Aktif, StringComparison.OrdinalIgnoreCase);
        }

        private static bool RequiresAccountantApproval(Kullanici? user)
        {
            return user != null && !IsApprovedAccountantUser(user);
        }

        private static void RequireApprovedAccountantUser(Kullanici? user)
        {
            if (RequiresAccountantApproval(user))
                throw new InvalidOperationException("Muhasebeci hesabı onay bekliyor.");
        }

        private async Task RequireAccountantParticipantAsync(CashTrackerDbContext db, int userId, int muhasebeciIsletmeId, CancellationToken ct)
        {
            var allowed = await db.IsletmeUyelikleri.AnyAsync(x =>
                x.KullaniciId == userId &&
                x.IsletmeId == muhasebeciIsletmeId &&
                x.Durum == "Aktif", ct);
            if (!allowed)
                throw new InvalidOperationException("Bu muhasebeci talebi için yetkiniz yok.");
        }

        private async Task RequireActiveLegacyBusinessParticipantAsync(CashTrackerDbContext db, int isletmeId, CancellationToken ct)
        {
            var active = await _isletmeService.GetActiveAsync();
            if (active.Id == isletmeId)
                return;

            throw new InvalidOperationException("Bu talep için yetkiniz yok.");
        }

        private async Task RequireActiveLegacyRequestParticipantAsync(
            CashTrackerDbContext db,
            MuhasebeciMusteriTalebi talep,
            CancellationToken ct)
        {
            var active = await _isletmeService.GetActiveAsync();
            if (active.Id == talep.MuhasebeciIsletmeId ||
                (talep.MusteriIsletmeId.HasValue && active.Id == talep.MusteriIsletmeId.Value))
            {
                return;
            }

            throw new InvalidOperationException("Bu talep için yetkiniz yok.");
        }

        private async Task RequireRequestParticipantAsync(
            CashTrackerDbContext db,
            int userId,
            MuhasebeciMusteriTalebi talep,
            CancellationToken ct)
        {
            var businessIds = await db.IsletmeUyelikleri.AsNoTracking()
                .Where(x => x.KullaniciId == userId && x.Durum == "Aktif")
                .Select(x => x.IsletmeId)
                .ToListAsync(ct);
            if (businessIds.Contains(talep.MuhasebeciIsletmeId) ||
                (talep.MusteriIsletmeId.HasValue && businessIds.Contains(talep.MusteriIsletmeId.Value)))
            {
                return;
            }

            throw new InvalidOperationException("Bu talep için yetkiniz yok.");
        }

        private static async Task<MuhasebeciMusteriTalebi> RequirePendingRequestAsync(CashTrackerDbContext db, int talepId, CancellationToken ct)
        {
            return await db.MuhasebeciMusteriTalepleri.FirstOrDefaultAsync(x =>
                x.Id == talepId &&
                x.Durum == MuhasebeciTalepDurumlari.Beklemede, ct)
                ?? throw new InvalidOperationException("Bekleyen talep bulunamadı.");
        }

        private static async Task EnsureNoActiveOrPendingPairAsync(
            CashTrackerDbContext db,
            int muhasebeciIsletmeId,
            int musteriIsletmeId,
            CancellationToken ct)
        {
            var connected = await db.MuhasebeciMusterileri.AnyAsync(x =>
                x.MuhasebeciIsletmeId == muhasebeciIsletmeId &&
                x.MusteriIsletmeId == musteriIsletmeId &&
                x.Durum == "Aktif", ct);
            if (connected)
                throw new InvalidOperationException("Bu muhasebeci ile zaten aktif baglanti var.");

            var pending = await db.MuhasebeciMusteriTalepleri.AnyAsync(x =>
                x.MuhasebeciIsletmeId == muhasebeciIsletmeId &&
                x.MusteriIsletmeId == musteriIsletmeId &&
                x.Durum == MuhasebeciTalepDurumlari.Beklemede, ct);
            if (pending)
                throw new InvalidOperationException("Bu muhasebeci için bekleyen talep var.");
        }

        private async Task<ConversationContext> RequireCustomerConversationAsync(
            CashTrackerDbContext db,
            int muhasebeciIsletmeId,
            int musteriIsletmeId,
            CancellationToken ct)
        {
            var relation = await db.MuhasebeciMusterileri.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.MuhasebeciIsletmeId == muhasebeciIsletmeId &&
                    x.MusteriIsletmeId == musteriIsletmeId &&
                    x.Durum == "Aktif", ct);
            if (relation != null)
                return new ConversationContext(muhasebeciIsletmeId, musteriIsletmeId, relation.TalepId, relation.Id, "Aktif bağlantı");

            var request = await db.MuhasebeciMusteriTalepleri.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.MuhasebeciIsletmeId == muhasebeciIsletmeId &&
                    x.MusteriIsletmeId == musteriIsletmeId &&
                    x.Durum == MuhasebeciTalepDurumlari.Beklemede, ct);
            if (request != null)
                return new ConversationContext(muhasebeciIsletmeId, musteriIsletmeId, request.Id, null, "Talep bekliyor");

            throw new InvalidOperationException("Bu muhasebeci ile uygulama içi sohbet başlatmak için önce talep veya bağlantı gerekir.");
        }

        private async Task<MuhasebeciMusteriTalebi> RequireAccountantConversationRequestAsync(
            CashTrackerDbContext db,
            int userId,
            int talepId,
            CancellationToken ct)
        {
            var talep = await db.MuhasebeciMusteriTalepleri.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == talepId &&
                    x.MusteriIsletmeId.HasValue &&
                    x.Durum == MuhasebeciTalepDurumlari.Beklemede, ct)
                ?? throw new InvalidOperationException("Sohbet edilecek bekleyen talep bulunamadı.");

            await RequireAccountantParticipantAsync(db, userId, talep.MuhasebeciIsletmeId, ct);
            return talep;
        }

        private static async Task<ConversationContext> RequireAccountantCustomerConversationAsync(
            CashTrackerDbContext db,
            int muhasebeciIsletmeId,
            int musteriIsletmeId,
            CancellationToken ct)
        {
            var relation = await db.MuhasebeciMusterileri.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.MuhasebeciIsletmeId == muhasebeciIsletmeId &&
                    x.MusteriIsletmeId == musteriIsletmeId &&
                    x.Durum == "Aktif", ct)
                ?? throw new InvalidOperationException("Aktif müşteri bağlantısı bulunamadı.");

            return new ConversationContext(muhasebeciIsletmeId, musteriIsletmeId, relation.TalepId, relation.Id, "Aktif bağlantı");
        }

        private static async Task AddConversationMessageAsync(
            CashTrackerDbContext db,
            ConversationContext conversation,
            int gonderenIsletmeId,
            string? mesaj,
            CancellationToken ct)
        {
            if (gonderenIsletmeId != conversation.MuhasebeciIsletmeId && gonderenIsletmeId != conversation.MusteriIsletmeId)
                throw new InvalidOperationException("Bu sohbet için yetkiniz yok.");

            var normalizedMessage = NormalizeConversationText(mesaj, allowEmpty: false);
            db.MuhasebeciSohbetMesajlari.Add(new MuhasebeciSohbetMesaji
            {
                MuhasebeciIsletmeId = conversation.MuhasebeciIsletmeId,
                MusteriIsletmeId = conversation.MusteriIsletmeId,
                GonderenIsletmeId = gonderenIsletmeId,
                TalepId = conversation.TalepId,
                BaglantiId = conversation.BaglantiId,
                Mesaj = normalizedMessage,
                CreatedAt = DateTime.Now
            });
            await db.SaveChangesAsync(ct);
        }

        private static async Task MarkConversationReadAsync(
            CashTrackerDbContext db,
            ConversationContext conversation,
            int viewerBusinessId,
            CancellationToken ct)
        {
            var unread = await db.MuhasebeciSohbetMesajlari
                .Where(x =>
                    x.MuhasebeciIsletmeId == conversation.MuhasebeciIsletmeId &&
                    x.MusteriIsletmeId == conversation.MusteriIsletmeId &&
                    x.GonderenIsletmeId != viewerBusinessId &&
                    x.OkunduAt == null)
                .ToListAsync(ct);

            if (unread.Count == 0)
                return;

            var now = DateTime.Now;
            foreach (var message in unread)
                message.OkunduAt = now;

            await db.SaveChangesAsync(ct);
        }

        private static async Task<MuhasebeciSohbetDto> BuildConversationDtoAsync(
            CashTrackerDbContext db,
            ConversationContext conversation,
            int viewerBusinessId,
            CancellationToken ct)
        {
            var businesses = await db.Isletmeler.AsNoTracking()
                .Where(x => x.Id == conversation.MuhasebeciIsletmeId || x.Id == conversation.MusteriIsletmeId)
                .ToDictionaryAsync(x => x.Id, ct);
            businesses.TryGetValue(conversation.MuhasebeciIsletmeId, out var accountant);
            businesses.TryGetValue(conversation.MusteriIsletmeId, out var customer);

            var messages = await db.MuhasebeciSohbetMesajlari.AsNoTracking()
                .Where(x =>
                    x.MuhasebeciIsletmeId == conversation.MuhasebeciIsletmeId &&
                    x.MusteriIsletmeId == conversation.MusteriIsletmeId)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);

            return new MuhasebeciSohbetDto
            {
                MuhasebeciIsletmeId = conversation.MuhasebeciIsletmeId,
                MusteriIsletmeId = conversation.MusteriIsletmeId,
                TalepId = conversation.TalepId,
                BaglantiId = conversation.BaglantiId,
                MuhasebeciAdi = DisplayName(accountant?.Ad, "Muhasebeci"),
                MusteriAdi = DisplayName(customer?.Ad, "Müşteri"),
                Durum = conversation.Durum,
                BilgiMesaji = "Telefon, e-posta, web adresi ve sosyal medya bilgileri ödeme akışı tamamlanana kadar Systemcel sohbetinde paylaşılmaz.",
                Mesajlar = messages.Select(x => new MuhasebeciSohbetMesajiDto
                {
                    Id = x.Id,
                    GonderenIsletmeId = x.GonderenIsletmeId,
                    GonderenAdi = x.GonderenIsletmeId == conversation.MuhasebeciIsletmeId
                        ? DisplayName(accountant?.Ad, "Muhasebeci")
                        : DisplayName(customer?.Ad, "Müşteri"),
                    BenimMesajim = x.GonderenIsletmeId == viewerBusinessId,
                    Mesaj = x.Mesaj,
                    CreatedAt = x.CreatedAt
                }).ToList()
            };
        }

        private static async Task AcceptTalepAsync(
            CashTrackerDbContext db,
            MuhasebeciMusteriTalebi talep,
            int musteriIsletmeId,
            string yetkiSeviyesi,
            CancellationToken ct)
        {
            var now = DateTime.Now;
            var relation = await db.MuhasebeciMusterileri.FirstOrDefaultAsync(x =>
                x.MuhasebeciIsletmeId == talep.MuhasebeciIsletmeId &&
                x.MusteriIsletmeId == musteriIsletmeId, ct);

            if (relation == null)
            {
                relation = new MuhasebeciMusteri
                {
                    MuhasebeciIsletmeId = talep.MuhasebeciIsletmeId,
                    MusteriIsletmeId = musteriIsletmeId,
                    CreatedAt = now
                };
                db.MuhasebeciMusterileri.Add(relation);
            }

            relation.Durum = "Aktif";
            relation.YetkiSeviyesi = NormalizeYetki(yetkiSeviyesi);
            relation.Kaynak = talep.Tur;
            relation.TalepId = talep.Id;
            relation.DavetKodu = string.IsNullOrWhiteSpace(talep.DavetKodu) ? relation.DavetKodu : talep.DavetKodu;
            relation.BaslangicAt = now;
            relation.BitisAt = null;
            relation.KabulAt = now;
            relation.UpdatedAt = now;

            talep.Durum = MuhasebeciTalepDurumlari.Kabul;
            talep.MusteriIsletmeId = musteriIsletmeId;
            talep.YetkiSeviyesi = relation.YetkiSeviyesi;
            talep.SonucAt = now;
            talep.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
        }

        private static async Task<HashSet<int>> GetActiveProAccountantIdsAsync(
            CashTrackerDbContext db,
            List<int> accountantIds,
            CancellationToken ct)
        {
            var now = DateTime.Now;
            return (await db.Abonelikler.AsNoTracking()
                    .Where(x => accountantIds.Contains(x.IsletmeId))
                    .Where(x => x.HesapTipi == HesapTipleri.Muhasebeci && x.PlanKodu == PlanKodlari.MuhasebeciPro)
                    .Where(x => x.Durum == "Aktif" && x.DonemBaslangicAt <= now && (x.DonemBitisAt == null || x.DonemBitisAt >= now))
                    .Select(x => x.IsletmeId)
                    .ToListAsync(ct))
                .ToHashSet();
        }

        private static async Task<string> GenerateInviteCodeAsync(CashTrackerDbContext db, CancellationToken ct)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var code = $"MUS-{Random.Shared.Next(100000, 999999)}";
                var exists = await db.MuhasebeciMusteriTalepleri.AnyAsync(x => x.DavetKodu == code, ct);
                if (!exists)
                    return code;
            }

            return $"MUS-{Guid.NewGuid():N}"[..14].ToUpperInvariant();
        }

        private static MuhasebeciProfilDto BuildProfileDto(
            Isletme accountant,
            MuhasebeciProfil? profile,
            SubscriptionEntitlementStatus entitlement,
            bool talepVar,
            bool bagli)
        {
            return BuildProfileDto(
                accountant,
                profile,
                entitlement.PlanAdi,
                string.Equals(entitlement.PlanKodu, PlanKodlari.MuhasebeciPro, StringComparison.OrdinalIgnoreCase),
                talepVar,
                bagli);
        }

        private static MuhasebeciProfilDto BuildProfileDto(
            Isletme accountant,
            MuhasebeciProfil? profile,
            string planAdi,
            bool pro,
            bool talepVar,
            bool bagli,
            bool telefonGoster = true)
        {
            return new MuhasebeciProfilDto
            {
                MuhasebeciIsletmeId = accountant.Id,
                Yayinda = profile?.Yayinda ?? false,
                Unvan = DisplayName(profile?.Unvan, DisplayName(accountant.Ad, "Muhasebeci")),
                Konum = DisplayName(profile?.Konum, accountant.Konum ?? string.Empty),
                Telefon = telefonGoster ? profile?.Telefon ?? string.Empty : string.Empty,
                DeneyimYili = profile?.DeneyimYili ?? 0,
                ProfilResmiUrl = profile?.ProfilResmiUrl ?? string.Empty,
                UcretBilgisi = profile?.UcretBilgisi ?? string.Empty,
                Uzmanliklar = DisplayName(profile?.Uzmanliklar, "Genel muhasebe"),
                MusteriTipleri = DisplayName(profile?.MusteriTipleri, "KOBİ ve küçük işletmeler"),
                KisaAciklama = DisplayName(profile?.KisaAciklama, "Gelir, gider, fatura ve dönem takibinde destek olur."),
                PlanAdi = planAdi,
                Pro = pro,
                TalepVar = talepVar,
                Bagli = bagli
            };
        }

        private sealed record ConversationContext(
            int MuhasebeciIsletmeId,
            int MusteriIsletmeId,
            int? TalepId,
            int? BaglantiId,
            string Durum);

        private async Task<MuhasebeciTalepDto> BuildTalepDtoAsync(CashTrackerDbContext db, MuhasebeciMusteriTalebi talep, CancellationToken ct)
        {
            var accountant = await db.Isletmeler.AsNoTracking().FirstOrDefaultAsync(x => x.Id == talep.MuhasebeciIsletmeId, ct);
            Isletme? customer = null;
            if (talep.MusteriIsletmeId.HasValue)
                customer = await db.Isletmeler.AsNoTracking().FirstOrDefaultAsync(x => x.Id == talep.MusteriIsletmeId.Value, ct);

            return BuildTalepDto(talep, accountant, customer);
        }

        private static MuhasebeciTalepDto BuildTalepDto(
            MuhasebeciMusteriTalebi talep,
            Isletme? accountant,
            Isletme? customer,
            string publicBaseUrl = "")
        {
            var inviteLink = string.IsNullOrWhiteSpace(talep.DavetKodu)
                ? string.Empty
                : $"{publicBaseUrl.TrimEnd('/')}/muhasebeciler?davet={Uri.EscapeDataString(talep.DavetKodu)}";

            return new MuhasebeciTalepDto
            {
                Id = talep.Id,
                MuhasebeciIsletmeId = talep.MuhasebeciIsletmeId,
                MusteriIsletmeId = talep.MusteriIsletmeId,
                MuhasebeciAdi = DisplayName(accountant?.Ad, "Muhasebeci"),
                MusteriAdi = DisplayName(customer?.Ad, string.Empty),
                Tur = talep.Tur,
                Durum = talep.Durum,
                YetkiSeviyesi = NormalizeYetki(talep.YetkiSeviyesi),
                DavetKodu = talep.DavetKodu,
                DavetLinki = inviteLink,
                Mesaj = talep.Mesaj,
                CreatedAt = talep.CreatedAt
            };
        }

        private static string NormalizeYetki(string? value)
        {
            return string.Equals(value?.Trim(), MuhasebeciYetkiSeviyeleri.TamIslem, StringComparison.OrdinalIgnoreCase)
                ? MuhasebeciYetkiSeviyeleri.TamIslem
                : MuhasebeciYetkiSeviyeleri.OkumaRapor;
        }

        private static string NormalizeInviteCode(string? value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string NormalizeText(string? value, string fallback)
        {
            var normalized = value?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
        }

        private static string NormalizeRequiredText(string? value, string fieldName)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                throw new InvalidOperationException($"{fieldName} zorunludur.");
            return normalized;
        }

        private static string NormalizeConversationText(string? value, bool allowEmpty)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                if (allowEmpty)
                    return string.Empty;

                throw new InvalidOperationException("Mesaj boş olamaz.");
            }

            if (normalized.Length > 1_000)
                throw new InvalidOperationException("Mesaj en fazla 1000 karakter olabilir.");

            if (ContainsDirectContactInfo(normalized))
                throw new InvalidOperationException("Telefon, e-posta, web adresi veya sosyal medya bilgisi ödeme tamamlanmadan paylaşılamaz. Lütfen iletişimi Systemcel sohbeti üzerinden sürdürün.");

            return normalized;
        }

        private static bool ContainsDirectContactInfo(string value)
        {
            return EmailRegex.IsMatch(value) ||
                UrlRegex.IsMatch(value) ||
                PhoneRegex.IsMatch(value) ||
                ContainsFragmentedPhoneNumber(value) ||
                ContainsNumberWordContact(value) ||
                SocialContactRegex.IsMatch(value);
        }

        private static bool ContainsFragmentedPhoneNumber(string value)
        {
            var digitGroups = DigitGroupRegex.Matches(value)
                .Select(x => x.Value)
                .Where(x => x.Length > 0)
                .ToList();
            if (digitGroups.Count < 2)
                return false;

            var digits = string.Concat(digitGroups);
            for (var i = 0; i < digits.Length; i++)
            {
                var remaining = digits[i..];
                if (remaining.Length >= 12 && remaining.StartsWith("90", StringComparison.Ordinal) && remaining[2] == '5')
                    return true;
                if (remaining.Length >= 11 && remaining[0] == '0' && remaining[1] == '5')
                    return true;
                if (remaining.Length >= 10 && remaining[0] == '5')
                    return true;
            }

            return false;
        }

        private static bool ContainsNumberWordContact(string value)
        {
            var normalized = NormalizeSearch(value);
            var count = NumberWordRegex.Matches(normalized).Count;
            return count >= 7;
        }

        private static string DisplayName(string? value, string fallback)
        {
            return NormalizeText(value, fallback);
        }

        private static string NormalizeSearch(string? value)
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

        private static readonly Regex EmailRegex = new(
            @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex UrlRegex = new(
            @"\b(?:https?://|www\.|[a-z0-9\-]+\.(?:com|net|org|io|co|tr|com\.tr|info|biz))\S*",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex PhoneRegex = new(
            @"(?<!\d)(?:\+?\d[\s().\-]*){7,}\d(?!\d)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex DigitGroupRegex = new(
            @"\d+",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex NumberWordRegex = new(
            @"\b(?:sifir|bir|iki|uc|dort|bes|alti|yedi|sekiz|dokuz)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex SocialContactRegex = new(
            @"\b(?:whatsapp|telegram|instagram|linkedin|facebook|gmail|hotmail|outlook|e-posta|eposta)\b|@[\p{L}\p{N}_\.]{3,}",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }
}
