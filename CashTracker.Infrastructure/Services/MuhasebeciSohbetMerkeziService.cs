using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
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
    public sealed class MuhasebeciSohbetMerkeziService : IMuhasebeciSohbetMerkeziService
    {
        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 50;
        private const long MaxAttachmentBytes = 10 * 1024 * 1024;
        private const int MaxAttachmentsPerMessage = 5;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".xml", ".html", ".htm", ".xlsx", ".csv", ".zip", ".png", ".jpg", ".jpeg", ".webp",
            ".webm", ".ogg", ".m4a", ".mp3", ".wav"
        };

        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;
        private readonly MuhasebeciSohbetStorageOptions _storageOptions;

        public MuhasebeciSohbetMerkeziService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService,
            MuhasebeciSohbetStorageOptions storageOptions)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
            _storageOptions = storageOptions;
        }

        public async Task<MuhasebeciSohbetListeDto> GetSohbetlerAsync(bool includeArchived = false, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var viewerBusinessId = await GetViewerBusinessIdAsync(ct);
            await EnsureRelevantConversationsAsync(db, viewerBusinessId, ct);

            var sohbetler = await db.MuhasebeciSohbetleri.AsNoTracking()
                .Where(x => x.MuhasebeciIsletmeId == viewerBusinessId || x.MusteriIsletmeId == viewerBusinessId)
                .OrderByDescending(x => x.SonMesajAt ?? x.UpdatedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);
            var sohbetIds = sohbetler.Select(x => x.Id).ToList();
            var participantStates = await db.MuhasebeciSohbetKatilimciDurumlari.AsNoTracking()
                .Where(x => sohbetIds.Contains(x.SohbetId) && x.IsletmeId == viewerBusinessId)
                .ToDictionaryAsync(x => x.SohbetId, ct);
            var messages = await db.MuhasebeciSohbetMesajlari.AsNoTracking()
                .Where(x => x.SohbetId.HasValue && sohbetIds.Contains(x.SohbetId.Value))
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);
            var businessIds = sohbetler.SelectMany(x => new[] { x.MuhasebeciIsletmeId, x.MusteriIsletmeId }).Distinct().ToList();
            var businesses = await db.Isletmeler.AsNoTracking()
                .Where(x => businessIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);

            var rows = sohbetler
                .Select(sohbet =>
                {
                    participantStates.TryGetValue(sohbet.Id, out var state);
                    var last = messages.FirstOrDefault(x => x.SohbetId == sohbet.Id);
                    return ToSohbetOzet(sohbet, viewerBusinessId, businesses, state, last, messages);
                })
                .Where(x => includeArchived || !x.Arsivlendi)
                .ToList();

            return new MuhasebeciSohbetListeDto
            {
                Sohbetler = rows,
                OkunmamisMesajSayisi = rows.Sum(x => x.OkunmamisMesajSayisi)
            };
        }

        public async Task<MuhasebeciSohbetMesajSayfasiDto> GetMesajlarAsync(int sohbetId, int? beforeId = null, int limit = DefaultPageSize, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var viewerBusinessId = await GetViewerBusinessIdAsync(ct);
            var sohbet = await RequireConversationAsync(db, sohbetId, viewerBusinessId, ct);
            await EnsureParticipantsAsync(db, sohbet, ct);
            await LinkLegacyMessagesAsync(db, sohbet, ct);
            await MarkReadAsync(db, sohbet, viewerBusinessId, ct);

            limit = Math.Clamp(limit <= 0 ? DefaultPageSize : limit, 1, MaxPageSize);
            var query = db.MuhasebeciSohbetMesajlari.AsNoTracking()
                .Where(x => x.SohbetId == sohbet.Id);
            if (beforeId.HasValue)
                query = query.Where(x => x.Id < beforeId.Value);

            var pageDescending = await query
                .OrderByDescending(x => x.Id)
                .Take(limit + 1)
                .ToListAsync(ct);
            var hasMore = pageDescending.Count > limit;
            var page = pageDescending.Take(limit).OrderBy(x => x.Id).ToList();
            var dtoMessages = await BuildMessageDtosAsync(db, page, viewerBusinessId, ct);
            var summary = (await BuildConversationSummariesAsync(db, new[] { sohbet }, viewerBusinessId, ct)).Single();

            return new MuhasebeciSohbetMesajSayfasiDto
            {
                SohbetId = sohbet.Id,
                Sohbet = summary,
                Mesajlar = dtoMessages,
                HasMore = hasMore,
                NextBeforeId = hasMore ? page.Min(x => x.Id) : null
            };
        }

        public async Task<MuhasebeciSohbetMerkeziMesajiDto> MesajGonderAsync(int sohbetId, MuhasebeciSohbetMesajiOlusturRequest request, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var viewerBusinessId = await GetViewerBusinessIdAsync(ct);
            var sohbet = await RequireConversationAsync(db, sohbetId, viewerBusinessId, ct);
            var clientMessageId = (request.ClientMessageId ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(clientMessageId))
            {
                var existing = await db.MuhasebeciSohbetMesajlari.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SohbetId == sohbet.Id && x.ClientMessageId == clientMessageId, ct);
                if (existing != null)
                    return (await BuildMessageDtosAsync(db, new[] { existing }, viewerBusinessId, ct)).Single();
            }

            var message = await AddMessageAsync(db, sohbet, viewerBusinessId, MuhasebeciSohbetMesajTipleri.Metin, request.Mesaj, clientMessageId, ct);
            return (await BuildMessageDtosAsync(db, new[] { message }, viewerBusinessId, ct)).Single();
        }

        public async Task<MuhasebeciSohbetEkiDto> DosyaEkleAsync(int sohbetId, SohbetDosyaYukleme upload, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var viewerBusinessId = await GetViewerBusinessIdAsync(ct);
            var sohbet = await RequireConversationAsync(db, sohbetId, viewerBusinessId, ct);
            ValidateUpload(upload);

            var directory = Path.Combine(GetStorageRoot(), "chat-attachments", sohbet.Id.ToString(CultureInfo.InvariantCulture));
            Directory.CreateDirectory(directory);
            var extension = Path.GetExtension(upload.DosyaAdi);
            var storedName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(directory, storedName);
            await using (var stream = File.Create(fullPath))
                await upload.Icerik.CopyToAsync(stream, ct);

            var message = await AddMessageAsync(db, sohbet, viewerBusinessId, MuhasebeciSohbetMesajTipleri.Dosya, $"Dosya eklendi: {Path.GetFileName(upload.DosyaAdi)}", string.Empty, ct);
            var attachment = new MuhasebeciSohbetEki
            {
                SohbetId = sohbet.Id,
                MesajId = message.Id,
                YukleyenIsletmeId = viewerBusinessId,
                EkTipi = MuhasebeciSohbetEkTipleri.Dosya,
                DosyaAdi = Path.GetFileName(upload.DosyaAdi),
                IcerikTipi = NormalizeContentType(upload.IcerikTipi, extension),
                DosyaYolu = fullPath,
                Boyut = upload.Boyut,
                CreatedAt = DateTime.Now
            };
            db.MuhasebeciSohbetEkleri.Add(attachment);
            await db.SaveChangesAsync(ct);
            return ToAttachmentDto(attachment);
        }

        public async Task<SohbetDosyaIndirme> DosyaIndirAsync(int ekId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var viewerBusinessId = await GetViewerBusinessIdAsync(ct);
            var attachment = await db.MuhasebeciSohbetEkleri.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == ekId, ct)
                ?? throw new InvalidOperationException("Ek bulunamadi.");
            await RequireConversationAsync(db, attachment.SohbetId, viewerBusinessId, ct);
            if (string.IsNullOrWhiteSpace(attachment.DosyaYolu) || !File.Exists(attachment.DosyaYolu))
                throw new FileNotFoundException("Dosya bulunamadi.", attachment.DosyaAdi);

            return new SohbetDosyaIndirme
            {
                DosyaAdi = attachment.DosyaAdi,
                IcerikTipi = string.IsNullOrWhiteSpace(attachment.IcerikTipi) ? "application/octet-stream" : attachment.IcerikTipi,
                DosyaYolu = attachment.DosyaYolu
            };
        }

        public async Task<MuhasebeciSohbetVeriIstegiDto> VeriIstegiOlusturAsync(int sohbetId, MuhasebeciSohbetVeriIstegiRequest request, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var viewerBusinessId = await GetViewerBusinessIdAsync(ct);
            var sohbet = await RequireConversationAsync(db, sohbetId, viewerBusinessId, ct);
            if (viewerBusinessId != sohbet.MuhasebeciIsletmeId)
                throw new InvalidOperationException("Veri istegini muhasebeci baslatabilir.");

            var range = ResolveRange(request.AralikKodu, request.Baslangic, request.Bitis);
            var dataRequest = new MuhasebeciSohbetVeriIstegi
            {
                SohbetId = sohbet.Id,
                IsteyenIsletmeId = viewerBusinessId,
                HedefIsletmeId = sohbet.MusteriIsletmeId,
                VeriTipi = NormalizeDataType(request.VeriTipi),
                AralikKodu = NormalizeRangeCode(request.AralikKodu),
                Baslangic = range.From,
                Bitis = range.To,
                Durum = CanAutoShareData(db, sohbet)
                    ? MuhasebeciSohbetVeriIstegiDurumlari.Paylasildi
                    : MuhasebeciSohbetVeriIstegiDurumlari.Beklemede,
                Mesaj = NormalizeOptionalMessage(request.Mesaj)
            };
            db.MuhasebeciSohbetVeriIstekleri.Add(dataRequest);
            await AddMessageAsync(db, sohbet, viewerBusinessId, MuhasebeciSohbetMesajTipleri.VeriIstegi, BuildDataRequestMessage(dataRequest), string.Empty, ct);
            await db.SaveChangesAsync(ct);

            if (dataRequest.Durum == MuhasebeciSohbetVeriIstegiDurumlari.Paylasildi)
            {
                var share = await AddDataShareMessageAsync(db, sohbet, sohbet.MusteriIsletmeId, dataRequest.VeriTipi, dataRequest.AralikKodu, dataRequest.Baslangic, dataRequest.Bitis, dataRequest.Mesaj, ct);
                var packageAttachment = await db.MuhasebeciSohbetEkleri.AsNoTracking()
                    .Where(x => x.MesajId == share.Id && x.EkTipi == MuhasebeciSohbetEkTipleri.RaporPaketi)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync(ct);
                if (packageAttachment != null)
                    dataRequest.SonucEkId = packageAttachment.Id;
                await db.SaveChangesAsync(ct);
            }

            return ToDataRequestDto(dataRequest);
        }

        public async Task<MuhasebeciSohbetMerkeziMesajiDto> VeriPaylasAsync(int sohbetId, MuhasebeciSohbetVeriPaylasimiRequest request, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var viewerBusinessId = await GetViewerBusinessIdAsync(ct);
            var sohbet = await RequireConversationAsync(db, sohbetId, viewerBusinessId, ct);
            if (viewerBusinessId != sohbet.MusteriIsletmeId && !CanAutoShareData(db, sohbet))
                throw new InvalidOperationException("Bu veriyi paylasmak icin aktif okuma/rapor yetkisi gerekir.");

            var range = ResolveRange(request.AralikKodu, request.Baslangic, request.Bitis);
            var message = await AddDataShareMessageAsync(
                db,
                sohbet,
                viewerBusinessId,
                NormalizeDataType(request.VeriTipi),
                NormalizeRangeCode(request.AralikKodu),
                range.From,
                range.To,
                NormalizeOptionalMessage(request.Mesaj),
                ct);

            if (request.VeriIstegiId.HasValue)
            {
                var dataRequest = await db.MuhasebeciSohbetVeriIstekleri
                    .FirstOrDefaultAsync(x => x.Id == request.VeriIstegiId.Value && x.SohbetId == sohbet.Id, ct);
                if (dataRequest != null)
                {
                    dataRequest.Durum = MuhasebeciSohbetVeriIstegiDurumlari.Paylasildi;
                    dataRequest.UpdatedAt = DateTime.Now;
                    await db.SaveChangesAsync(ct);
                }
            }

            return (await BuildMessageDtosAsync(db, new[] { message }, viewerBusinessId, ct)).Single();
        }

        public async Task<MuhasebeciSohbetOzetDto> KonuGuncelleAsync(int sohbetId, MuhasebeciSohbetKonuGuncelleRequest request, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var viewerBusinessId = await GetViewerBusinessIdAsync(ct);
            var sohbet = await RequireConversationAsync(db, sohbetId, viewerBusinessId, ct);
            sohbet.Konu = NormalizeTopic(request.Konu);
            sohbet.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync(ct);
            return (await BuildConversationSummariesAsync(db, new[] { sohbet }, viewerBusinessId, ct)).Single();
        }

        public async Task<MuhasebeciSohbetOzetDto> ArsivleAsync(int sohbetId, MuhasebeciSohbetArsivRequest request, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var viewerBusinessId = await GetViewerBusinessIdAsync(ct);
            var sohbet = await RequireConversationAsync(db, sohbetId, viewerBusinessId, ct);
            await EnsureParticipantsAsync(db, sohbet, ct);
            var state = await db.MuhasebeciSohbetKatilimciDurumlari
                .FirstAsync(x => x.SohbetId == sohbet.Id && x.IsletmeId == viewerBusinessId, ct);
            state.Arsivlendi = request.Arsivlendi;
            state.ArsivlendiAt = request.Arsivlendi ? DateTime.Now : null;
            state.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync(ct);
            return (await BuildConversationSummariesAsync(db, new[] { sohbet }, viewerBusinessId, ct)).Single();
        }

        public async Task<int> GetOrCreateForCustomerAsync(int muhasebeciIsletmeId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var customer = await _isletmeService.GetActiveAsync();
            if (!string.Equals(customer.TenantTipi, HesapTipleri.Isletme, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Sohbet icin aktif hesap isletme olmalidir.");

            var relation = await db.MuhasebeciMusterileri.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MuhasebeciIsletmeId == muhasebeciIsletmeId && x.MusteriIsletmeId == customer.Id && x.Durum == "Aktif", ct);
            var request = relation == null
                ? await db.MuhasebeciMusteriTalepleri.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.MuhasebeciIsletmeId == muhasebeciIsletmeId && x.MusteriIsletmeId == customer.Id && x.Durum == MuhasebeciTalepDurumlari.Beklemede, ct)
                : null;
            if (relation == null && request == null)
                throw new InvalidOperationException("Bu muhasebeci ile sohbet baslatmak icin once talep veya baglanti gerekir.");

            var sohbet = await EnsureConversationAsync(db, muhasebeciIsletmeId, customer.Id, relation?.TalepId ?? request?.Id, relation?.Id, relation != null ? "Aktif baglanti" : "Talep bekliyor", ct);
            return sohbet.Id;
        }

        public async Task<int> GetOrCreateForAccountantRequestAsync(int talepId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var viewerBusinessId = await GetViewerBusinessIdAsync(ct);
            var talep = await db.MuhasebeciMusteriTalepleri.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == talepId && x.MusteriIsletmeId.HasValue && x.MuhasebeciIsletmeId == viewerBusinessId && x.Durum == MuhasebeciTalepDurumlari.Beklemede, ct)
                ?? throw new InvalidOperationException("Sohbet edilecek bekleyen talep bulunamadi.");
            var sohbet = await EnsureConversationAsync(db, talep.MuhasebeciIsletmeId, talep.MusteriIsletmeId!.Value, talep.Id, null, "Talep bekliyor", ct);
            return sohbet.Id;
        }

        public async Task<int> GetOrCreateForAccountantCustomerAsync(int musteriIsletmeId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var viewerBusinessId = await GetViewerBusinessIdAsync(ct);
            var relation = await db.MuhasebeciMusterileri.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MuhasebeciIsletmeId == viewerBusinessId && x.MusteriIsletmeId == musteriIsletmeId && x.Durum == "Aktif", ct)
                ?? throw new InvalidOperationException("Aktif musteri baglantisi bulunamadi.");
            var sohbet = await EnsureConversationAsync(db, relation.MuhasebeciIsletmeId, relation.MusteriIsletmeId, relation.TalepId, relation.Id, "Aktif baglanti", ct);
            return sohbet.Id;
        }

        private async Task<int> GetViewerBusinessIdAsync(CancellationToken ct)
        {
            var active = await _isletmeService.GetActiveAsync();
            var access = await _isletmeService.GetActiveAccessAsync();
            return access.MuhasebeciMusteriBaglami && access.MuhasebeciIsletmeId.HasValue
                ? access.MuhasebeciIsletmeId.Value
                : active.Id;
        }

        private static async Task<MuhasebeciSohbet> RequireConversationAsync(CashTrackerDbContext db, int sohbetId, int viewerBusinessId, CancellationToken ct)
        {
            var sohbet = await db.MuhasebeciSohbetleri
                .FirstOrDefaultAsync(x => x.Id == sohbetId, ct)
                ?? throw new InvalidOperationException("Sohbet bulunamadi.");
            if (sohbet.MuhasebeciIsletmeId != viewerBusinessId && sohbet.MusteriIsletmeId != viewerBusinessId)
                throw new InvalidOperationException("Bu sohbet icin yetkiniz yok.");
            return sohbet;
        }

        private async Task EnsureRelevantConversationsAsync(CashTrackerDbContext db, int viewerBusinessId, CancellationToken ct)
        {
            var relations = await db.MuhasebeciMusterileri.AsNoTracking()
                .Where(x => x.Durum == "Aktif" && (x.MuhasebeciIsletmeId == viewerBusinessId || x.MusteriIsletmeId == viewerBusinessId))
                .ToListAsync(ct);
            foreach (var relation in relations)
                await EnsureConversationAsync(db, relation.MuhasebeciIsletmeId, relation.MusteriIsletmeId, relation.TalepId, relation.Id, "Aktif baglanti", ct);

            var requests = await db.MuhasebeciMusteriTalepleri.AsNoTracking()
                .Where(x => x.MusteriIsletmeId.HasValue && x.Durum == MuhasebeciTalepDurumlari.Beklemede &&
                    (x.MuhasebeciIsletmeId == viewerBusinessId || x.MusteriIsletmeId == viewerBusinessId))
                .ToListAsync(ct);
            foreach (var request in requests)
                await EnsureConversationAsync(db, request.MuhasebeciIsletmeId, request.MusteriIsletmeId!.Value, request.Id, null, "Talep bekliyor", ct);

            var pairs = await db.MuhasebeciSohbetMesajlari.AsNoTracking()
                .Where(x => x.MuhasebeciIsletmeId == viewerBusinessId || x.MusteriIsletmeId == viewerBusinessId)
                .GroupBy(x => new { x.MuhasebeciIsletmeId, x.MusteriIsletmeId })
                .Select(x => new
                {
                    x.Key.MuhasebeciIsletmeId,
                    x.Key.MusteriIsletmeId,
                    TalepId = x.Max(m => m.TalepId),
                    BaglantiId = x.Max(m => m.BaglantiId),
                    LastAt = x.Max(m => m.CreatedAt)
                })
                .ToListAsync(ct);
            foreach (var pair in pairs)
            {
                var sohbet = await EnsureConversationAsync(db, pair.MuhasebeciIsletmeId, pair.MusteriIsletmeId, pair.TalepId, pair.BaglantiId, pair.BaglantiId.HasValue ? "Aktif baglanti" : "Talep bekliyor", ct);
                if (!sohbet.SonMesajAt.HasValue || pair.LastAt > sohbet.SonMesajAt.Value)
                {
                    sohbet.SonMesajAt = pair.LastAt;
                    sohbet.UpdatedAt = DateTime.Now;
                }
            }

            await db.SaveChangesAsync(ct);
        }

        private static async Task<MuhasebeciSohbet> EnsureConversationAsync(
            CashTrackerDbContext db,
            int muhasebeciIsletmeId,
            int musteriIsletmeId,
            int? talepId,
            int? baglantiId,
            string fallbackTopic,
            CancellationToken ct)
        {
            var sohbet = await db.MuhasebeciSohbetleri
                .FirstOrDefaultAsync(x => x.MuhasebeciIsletmeId == muhasebeciIsletmeId && x.MusteriIsletmeId == musteriIsletmeId, ct);
            if (sohbet == null)
            {
                sohbet = new MuhasebeciSohbet
                {
                    MuhasebeciIsletmeId = muhasebeciIsletmeId,
                    MusteriIsletmeId = musteriIsletmeId,
                    TalepId = talepId,
                    BaglantiId = baglantiId,
                    Konu = fallbackTopic,
                    Durum = "Aktif",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                db.MuhasebeciSohbetleri.Add(sohbet);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                var changed = false;
                if (!sohbet.TalepId.HasValue && talepId.HasValue)
                {
                    sohbet.TalepId = talepId;
                    changed = true;
                }
                if (!sohbet.BaglantiId.HasValue && baglantiId.HasValue)
                {
                    sohbet.BaglantiId = baglantiId;
                    changed = true;
                }
                if (changed)
                {
                    sohbet.UpdatedAt = DateTime.Now;
                    await db.SaveChangesAsync(ct);
                }
            }

            await EnsureParticipantsAsync(db, sohbet, ct);
            await LinkLegacyMessagesAsync(db, sohbet, ct);
            return sohbet;
        }

        private static async Task EnsureParticipantsAsync(CashTrackerDbContext db, MuhasebeciSohbet sohbet, CancellationToken ct)
        {
            await EnsureParticipantAsync(db, sohbet.Id, sohbet.MuhasebeciIsletmeId, ct);
            await EnsureParticipantAsync(db, sohbet.Id, sohbet.MusteriIsletmeId, ct);
        }

        private static async Task EnsureParticipantAsync(CashTrackerDbContext db, int sohbetId, int isletmeId, CancellationToken ct)
        {
            var exists = await db.MuhasebeciSohbetKatilimciDurumlari.AnyAsync(x => x.SohbetId == sohbetId && x.IsletmeId == isletmeId, ct);
            if (exists)
                return;

            db.MuhasebeciSohbetKatilimciDurumlari.Add(new MuhasebeciSohbetKatilimciDurumu
            {
                SohbetId = sohbetId,
                IsletmeId = isletmeId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
            await db.SaveChangesAsync(ct);
        }

        private static async Task LinkLegacyMessagesAsync(CashTrackerDbContext db, MuhasebeciSohbet sohbet, CancellationToken ct)
        {
            var legacy = await db.MuhasebeciSohbetMesajlari
                .Where(x => x.SohbetId == null && x.MuhasebeciIsletmeId == sohbet.MuhasebeciIsletmeId && x.MusteriIsletmeId == sohbet.MusteriIsletmeId)
                .ToListAsync(ct);
            if (legacy.Count == 0)
                return;

            foreach (var message in legacy)
            {
                message.SohbetId = sohbet.Id;
                if (string.IsNullOrWhiteSpace(message.MesajTipi))
                    message.MesajTipi = MuhasebeciSohbetMesajTipleri.Metin;
                message.ClientMessageId ??= string.Empty;
            }

            var lastAt = legacy.Max(x => x.CreatedAt);
            if (!sohbet.SonMesajAt.HasValue || lastAt > sohbet.SonMesajAt.Value)
                sohbet.SonMesajAt = lastAt;
            sohbet.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync(ct);
        }

        private static async Task MarkReadAsync(CashTrackerDbContext db, MuhasebeciSohbet sohbet, int viewerBusinessId, CancellationToken ct)
        {
            var unread = await db.MuhasebeciSohbetMesajlari
                .Where(x => x.SohbetId == sohbet.Id && x.GonderenIsletmeId != viewerBusinessId && x.OkunduAt == null)
                .ToListAsync(ct);
            var now = DateTime.Now;
            foreach (var message in unread)
                message.OkunduAt = now;

            var state = await db.MuhasebeciSohbetKatilimciDurumlari
                .FirstOrDefaultAsync(x => x.SohbetId == sohbet.Id && x.IsletmeId == viewerBusinessId, ct);
            if (state != null)
            {
                state.SonOkumaAt = now;
                state.Arsivlendi = false;
                state.ArsivlendiAt = null;
                state.UpdatedAt = now;
            }

            if (unread.Count > 0 || state != null)
                await db.SaveChangesAsync(ct);
        }

        private async Task<MuhasebeciSohbetMesaji> AddMessageAsync(
            CashTrackerDbContext db,
            MuhasebeciSohbet sohbet,
            int senderBusinessId,
            string messageType,
            string message,
            string clientMessageId,
            CancellationToken ct)
        {
            if (senderBusinessId != sohbet.MuhasebeciIsletmeId && senderBusinessId != sohbet.MusteriIsletmeId)
                throw new InvalidOperationException("Bu sohbet icin yetkiniz yok.");

            var normalized = messageType == MuhasebeciSohbetMesajTipleri.Sistem
                ? NormalizeSystemMessage(message)
                : NormalizeConversationText(message);
            var entity = new MuhasebeciSohbetMesaji
            {
                SohbetId = sohbet.Id,
                MuhasebeciIsletmeId = sohbet.MuhasebeciIsletmeId,
                MusteriIsletmeId = sohbet.MusteriIsletmeId,
                GonderenIsletmeId = senderBusinessId,
                TalepId = sohbet.TalepId,
                BaglantiId = sohbet.BaglantiId,
                MesajTipi = messageType,
                ClientMessageId = clientMessageId,
                Mesaj = normalized,
                CreatedAt = DateTime.Now
            };
            db.MuhasebeciSohbetMesajlari.Add(entity);
            sohbet.SonMesajAt = entity.CreatedAt;
            sohbet.UpdatedAt = entity.CreatedAt;

            var otherBusinessId = senderBusinessId == sohbet.MuhasebeciIsletmeId ? sohbet.MusteriIsletmeId : sohbet.MuhasebeciIsletmeId;
            var otherState = await db.MuhasebeciSohbetKatilimciDurumlari
                .FirstOrDefaultAsync(x => x.SohbetId == sohbet.Id && x.IsletmeId == otherBusinessId, ct);
            if (otherState != null)
            {
                otherState.Arsivlendi = false;
                otherState.ArsivlendiAt = null;
                otherState.UpdatedAt = entity.CreatedAt;
            }

            await db.SaveChangesAsync(ct);
            return entity;
        }

        private async Task<MuhasebeciSohbetMesaji> AddDataShareMessageAsync(
            CashTrackerDbContext db,
            MuhasebeciSohbet sohbet,
            int senderBusinessId,
            string dataType,
            string rangeCode,
            DateTime from,
            DateTime to,
            string note,
            CancellationToken ct)
        {
            var package = await BuildDataPackageAsync(db, sohbet.MusteriIsletmeId, dataType, rangeCode, from, to, ct);
            var message = await AddMessageAsync(db, sohbet, senderBusinessId, MuhasebeciSohbetMesajTipleri.VeriPaylasimi, $"{package.Title} paylasildi.", string.Empty, ct);
            db.MuhasebeciSohbetEkleri.Add(new MuhasebeciSohbetEki
            {
                SohbetId = sohbet.Id,
                MesajId = message.Id,
                YukleyenIsletmeId = senderBusinessId,
                EkTipi = MuhasebeciSohbetEkTipleri.VeriKarti,
                DosyaAdi = string.Empty,
                IcerikTipi = "application/json",
                DosyaYolu = string.Empty,
                Boyut = 0,
                VeriTipi = dataType,
                Baslik = package.Title,
                OzetJson = package.SummaryJson,
                CreatedAt = DateTime.Now
            });
            db.MuhasebeciSohbetEkleri.Add(new MuhasebeciSohbetEki
            {
                SohbetId = sohbet.Id,
                MesajId = message.Id,
                YukleyenIsletmeId = senderBusinessId,
                EkTipi = MuhasebeciSohbetEkTipleri.RaporPaketi,
                DosyaAdi = Path.GetFileName(package.ZipPath),
                IcerikTipi = "application/zip",
                DosyaYolu = package.ZipPath,
                Boyut = File.Exists(package.ZipPath) ? new FileInfo(package.ZipPath).Length : 0,
                VeriTipi = dataType,
                Baslik = $"{package.Title} rapor paketi",
                OzetJson = package.SummaryJson,
                CreatedAt = DateTime.Now
            });
            db.MuhasebeciSohbetEkleri.Add(new MuhasebeciSohbetEki
            {
                SohbetId = sohbet.Id,
                MesajId = message.Id,
                YukleyenIsletmeId = senderBusinessId,
                EkTipi = MuhasebeciSohbetEkTipleri.RaporPaketi,
                DosyaAdi = Path.GetFileName(package.PdfPath),
                IcerikTipi = "application/pdf",
                DosyaYolu = package.PdfPath,
                Boyut = File.Exists(package.PdfPath) ? new FileInfo(package.PdfPath).Length : 0,
                VeriTipi = dataType,
                Baslik = $"{package.Title} harcama detaylari PDF",
                OzetJson = package.SummaryJson,
                CreatedAt = DateTime.Now
            });
            await db.SaveChangesAsync(ct);
            return message;
        }

        private async Task<DataPackage> BuildDataPackageAsync(CashTrackerDbContext db, int businessId, string dataType, string rangeCode, DateTime from, DateTime to, CancellationToken ct)
        {
            var records = await db.Kasalar.AsNoTracking()
                .Where(x => x.IsletmeId == businessId && x.Tarih >= from && x.Tarih <= to)
                .OrderBy(x => x.Tarih)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);
            var business = await db.Isletmeler.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId, ct);
            var income = records.Where(x => string.Equals(x.Tip, "Gelir", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Tip, "Giris", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Tutar);
            var expense = records.Where(x => string.Equals(x.Tip, "Gider", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Tip, "Cikis", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Tutar);
            var summary = new
            {
                veriTipi = dataType,
                aralikKodu = rangeCode,
                baslangic = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                bitis = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                isletme = business?.Ad ?? "Isletme",
                gelir = income,
                gider = expense,
                net = income - expense,
                gelirKayitSayisi = records.Count(x => string.Equals(x.Tip, "Gelir", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Tip, "Giris", StringComparison.OrdinalIgnoreCase)),
                giderKayitSayisi = records.Count(x => string.Equals(x.Tip, "Gider", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Tip, "Cikis", StringComparison.OrdinalIgnoreCase))
            };

            var title = $"{DisplayRange(rangeCode, from, to)} gelir/gider ozeti";
            var directory = Path.Combine(GetStorageRoot(), "chat-reports", businessId.ToString(CultureInfo.InvariantCulture), DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(directory);
            var csvPath = Path.Combine(directory, "gelir-gider.csv");
            var htmlPath = Path.Combine(directory, "ozet.html");
            var pdfPath = Path.Combine(directory, "harcama-detaylari.pdf");
            var zipPath = Path.Combine(directory, "rapor-paketi.zip");
            await File.WriteAllTextAsync(csvPath, BuildCsv(records), Encoding.UTF8, ct);
            await File.WriteAllTextAsync(htmlPath, BuildHtml(title, summary), Encoding.UTF8, ct);
            await File.WriteAllBytesAsync(pdfPath, BuildExpenseDetailsPdf(title, business?.Ad ?? "Isletme", records), ct);
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(csvPath, Path.GetFileName(csvPath));
                archive.CreateEntryFromFile(htmlPath, Path.GetFileName(htmlPath));
                archive.CreateEntryFromFile(pdfPath, Path.GetFileName(pdfPath));
            }

            return new DataPackage(title, JsonSerializer.Serialize(summary), zipPath, pdfPath);
        }

        private async Task<List<MuhasebeciSohbetOzetDto>> BuildConversationSummariesAsync(CashTrackerDbContext db, IEnumerable<MuhasebeciSohbet> conversations, int viewerBusinessId, CancellationToken ct)
        {
            var list = conversations.ToList();
            var ids = list.Select(x => x.Id).ToList();
            var messages = await db.MuhasebeciSohbetMesajlari.AsNoTracking()
                .Where(x => x.SohbetId.HasValue && ids.Contains(x.SohbetId.Value))
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);
            var states = await db.MuhasebeciSohbetKatilimciDurumlari.AsNoTracking()
                .Where(x => ids.Contains(x.SohbetId) && x.IsletmeId == viewerBusinessId)
                .ToDictionaryAsync(x => x.SohbetId, ct);
            var businessIds = list.SelectMany(x => new[] { x.MuhasebeciIsletmeId, x.MusteriIsletmeId }).Distinct().ToList();
            var businesses = await db.Isletmeler.AsNoTracking()
                .Where(x => businessIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);

            return list.Select(sohbet =>
            {
                states.TryGetValue(sohbet.Id, out var state);
                var last = messages.FirstOrDefault(x => x.SohbetId == sohbet.Id);
                return ToSohbetOzet(sohbet, viewerBusinessId, businesses, state, last, messages);
            }).ToList();
        }

        private static MuhasebeciSohbetOzetDto ToSohbetOzet(
            MuhasebeciSohbet sohbet,
            int viewerBusinessId,
            IReadOnlyDictionary<int, Isletme> businesses,
            MuhasebeciSohbetKatilimciDurumu? state,
            MuhasebeciSohbetMesaji? last,
            IReadOnlyList<MuhasebeciSohbetMesaji> allMessages)
        {
            businesses.TryGetValue(sohbet.MuhasebeciIsletmeId, out var accountant);
            businesses.TryGetValue(sohbet.MusteriIsletmeId, out var customer);
            var viewerIsAccountant = sohbet.MuhasebeciIsletmeId == viewerBusinessId;
            var counterparty = viewerIsAccountant ? customer : accountant;
            var title = DisplayName(counterparty?.Ad, viewerIsAccountant ? "Musteri" : "Muhasebeci");

            return new MuhasebeciSohbetOzetDto
            {
                Id = sohbet.Id,
                MuhasebeciIsletmeId = sohbet.MuhasebeciIsletmeId,
                MusteriIsletmeId = sohbet.MusteriIsletmeId,
                TalepId = sohbet.TalepId,
                BaglantiId = sohbet.BaglantiId,
                Baslik = title,
                Konu = string.IsNullOrWhiteSpace(sohbet.Konu) ? (sohbet.BaglantiId.HasValue ? "Aktif baglanti" : "Talep bekliyor") : sohbet.Konu,
                KarsiTarafAdi = title,
                Durum = sohbet.Durum,
                SonMesaj = last?.Mesaj ?? string.Empty,
                SonMesajAt = last?.CreatedAt ?? sohbet.SonMesajAt,
                OkunmamisMesajSayisi = allMessages.Count(x => x.SohbetId == sohbet.Id && x.GonderenIsletmeId != viewerBusinessId && x.OkunduAt == null),
                Arsivlendi = state?.Arsivlendi ?? false,
                HedefUrl = $"/app/sohbetler?sohbetId={sohbet.Id}"
            };
        }

        private static async Task<List<MuhasebeciSohbetMerkeziMesajiDto>> BuildMessageDtosAsync(CashTrackerDbContext db, IReadOnlyCollection<MuhasebeciSohbetMesaji> messages, int viewerBusinessId, CancellationToken ct)
        {
            if (messages.Count == 0)
                return new List<MuhasebeciSohbetMerkeziMesajiDto>();

            var messageIds = messages.Select(x => x.Id).ToList();
            var businessIds = messages.Select(x => x.GonderenIsletmeId).Distinct().ToList();
            var businesses = await db.Isletmeler.AsNoTracking()
                .Where(x => businessIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);
            var attachments = await db.MuhasebeciSohbetEkleri.AsNoTracking()
                .Where(x => x.MesajId.HasValue && messageIds.Contains(x.MesajId.Value))
                .OrderBy(x => x.Id)
                .ToListAsync(ct);
            var attachmentsByMessage = attachments
                .GroupBy(x => x.MesajId!.Value)
                .ToDictionary(x => x.Key, x => x.Select(ToAttachmentDto).ToList());

            return messages.OrderBy(x => x.Id).Select(x =>
            {
                businesses.TryGetValue(x.GonderenIsletmeId, out var sender);
                attachmentsByMessage.TryGetValue(x.Id, out var ekler);
                return new MuhasebeciSohbetMerkeziMesajiDto
                {
                    Id = x.Id,
                    SohbetId = x.SohbetId ?? 0,
                    GonderenIsletmeId = x.GonderenIsletmeId,
                    GonderenAdi = DisplayName(sender?.Ad, "Kullanici"),
                    BenimMesajim = x.GonderenIsletmeId == viewerBusinessId,
                    MesajTipi = string.IsNullOrWhiteSpace(x.MesajTipi) ? MuhasebeciSohbetMesajTipleri.Metin : x.MesajTipi,
                    ClientMessageId = x.ClientMessageId ?? string.Empty,
                    Mesaj = x.Mesaj,
                    Durum = x.OkunduAt.HasValue ? "Okundu" : "Gonderildi",
                    OkunduAt = x.OkunduAt,
                    CreatedAt = x.CreatedAt,
                    Ekler = ekler ?? new List<MuhasebeciSohbetEkiDto>()
                };
            }).ToList();
        }

        private static MuhasebeciSohbetEkiDto ToAttachmentDto(MuhasebeciSohbetEki attachment)
        {
            return new MuhasebeciSohbetEkiDto
            {
                Id = attachment.Id,
                MesajId = attachment.MesajId,
                EkTipi = attachment.EkTipi,
                DosyaAdi = attachment.DosyaAdi,
                IcerikTipi = attachment.IcerikTipi,
                Boyut = attachment.Boyut,
                VeriTipi = attachment.VeriTipi,
                Baslik = attachment.Baslik,
                OzetJson = attachment.OzetJson,
                IndirUrl = attachment.Id > 0 && !string.IsNullOrWhiteSpace(attachment.DosyaYolu)
                    ? $"/api/ekran/sohbet-ekleri/{attachment.Id}/indir"
                    : string.Empty,
                CreatedAt = attachment.CreatedAt
            };
        }

        private static MuhasebeciSohbetVeriIstegiDto ToDataRequestDto(MuhasebeciSohbetVeriIstegi request)
        {
            return new MuhasebeciSohbetVeriIstegiDto
            {
                Id = request.Id,
                SohbetId = request.SohbetId,
                VeriTipi = request.VeriTipi,
                AralikKodu = request.AralikKodu,
                Baslangic = request.Baslangic,
                Bitis = request.Bitis,
                Durum = request.Durum,
                SonucEkId = request.SonucEkId,
                Mesaj = request.Mesaj,
                CreatedAt = request.CreatedAt
            };
        }

        private static bool CanAutoShareData(CashTrackerDbContext db, MuhasebeciSohbet sohbet)
        {
            return db.MuhasebeciMusterileri.AsNoTracking().Any(x =>
                x.MuhasebeciIsletmeId == sohbet.MuhasebeciIsletmeId &&
                x.MusteriIsletmeId == sohbet.MusteriIsletmeId &&
                x.Durum == "Aktif" &&
                (x.YetkiSeviyesi == MuhasebeciYetkiSeviyeleri.OkumaRapor || x.YetkiSeviyesi == MuhasebeciYetkiSeviyeleri.TamIslem));
        }

        private static string NormalizeTopic(string? value)
        {
            var normalized = NormalizeConversationText(value);
            if (normalized.Length > 120)
                throw new InvalidOperationException("Konu en fazla 120 karakter olabilir.");
            return normalized;
        }

        private static string NormalizeConversationText(string? value)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                throw new InvalidOperationException("Mesaj bos olamaz.");
            if (normalized.Length > 1_000)
                throw new InvalidOperationException("Mesaj en fazla 1000 karakter olabilir.");
            if (ContainsDirectContactInfo(normalized))
                throw new InvalidOperationException("Telefon, e-posta, web adresi veya sosyal medya bilgisi paylasilamaz. Lutfen iletisimi Systemcel sohbeti uzerinden surdurun.");
            return normalized;
        }

        private static string NormalizeOptionalMessage(string? value)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return string.Empty;
            return NormalizeConversationText(normalized);
        }

        private static string NormalizeSystemMessage(string? value)
        {
            var normalized = (value ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(normalized) ? "Sistem mesaji" : normalized;
        }

        private static bool ContainsDirectContactInfo(string value)
        {
            return EmailRegex.IsMatch(value) ||
                UrlRegex.IsMatch(value) ||
                PhoneRegex.IsMatch(value) ||
                ContainsFragmentedPhoneNumber(value) ||
                SocialContactRegex.IsMatch(value);
        }

        private static bool ContainsFragmentedPhoneNumber(string value)
        {
            var digits = string.Concat(DigitGroupRegex.Matches(value).Select(x => x.Value));
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

        private static (DateTime From, DateTime To) ResolveRange(string? rangeCode, string? fromRaw, string? toRaw)
        {
            var today = DateTime.Today;
            var code = NormalizeRangeCode(rangeCode);
            return code switch
            {
                "thisMonth" => (new DateTime(today.Year, today.Month, 1), new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1)),
                "previousMonth" => (new DateTime(today.Year, today.Month, 1).AddMonths(-1), new DateTime(today.Year, today.Month, 1).AddDays(-1)),
                "selectedMonth" => ResolveSelectedMonth(fromRaw),
                "custom" => ResolveCustomRange(fromRaw, toRaw),
                _ => (today.AddDays(-29), today)
            };
        }

        private static (DateTime From, DateTime To) ResolveSelectedMonth(string? monthRaw)
        {
            if (!DateTime.TryParseExact((monthRaw ?? string.Empty).Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month))
                throw new InvalidOperationException("Secili ay yyyy-MM formatinda olmalidir.");
            var start = new DateTime(month.Year, month.Month, 1);
            return (start, start.AddMonths(1).AddDays(-1));
        }

        private static (DateTime From, DateTime To) ResolveCustomRange(string? fromRaw, string? toRaw)
        {
            if (!DateTime.TryParseExact((fromRaw ?? string.Empty).Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var from) ||
                !DateTime.TryParseExact((toRaw ?? string.Empty).Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var to))
                throw new InvalidOperationException("Ozel aralik icin baslangic ve bitis tarihi yyyy-MM-dd formatinda olmalidir.");
            if (from > to)
                throw new InvalidOperationException("Baslangic tarihi bitis tarihinden sonra olamaz.");
            return (from, to);
        }

        private static string NormalizeRangeCode(string? rangeCode)
        {
            var code = (rangeCode ?? string.Empty).Trim();
            return code is "thisMonth" or "previousMonth" or "selectedMonth" or "custom" ? code : "last30";
        }

        private static string NormalizeDataType(string? value)
        {
            var normalized = (value ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(normalized) ? "GelirGiderOzeti" : normalized;
        }

        private static string BuildDataRequestMessage(MuhasebeciSohbetVeriIstegi request)
        {
            return $"{DisplayRange(request.AralikKodu, request.Baslangic, request.Bitis)} verisi istendi.";
        }

        private static string DisplayRange(string rangeCode, DateTime from, DateTime to)
        {
            return rangeCode switch
            {
                "thisMonth" => "Bu ay",
                "previousMonth" => "Onceki ay",
                "selectedMonth" => from.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                "custom" => $"{from:dd.MM.yyyy} - {to:dd.MM.yyyy}",
                _ => "Son 30 gun"
            };
        }

        private string GetStorageRoot()
        {
            var root = string.IsNullOrWhiteSpace(_storageOptions.AppDataPath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Systemcel", "Web")
                : _storageOptions.AppDataPath;
            Directory.CreateDirectory(root);
            return root;
        }

        private static void ValidateUpload(SohbetDosyaYukleme upload)
        {
            if (upload == null || upload.Icerik == Stream.Null)
                throw new InvalidOperationException("Dosya secilmedi.");
            if (upload.Boyut <= 0)
                throw new InvalidOperationException("Dosya bos olamaz.");
            if (upload.Boyut > MaxAttachmentBytes)
                throw new InvalidOperationException("Dosya en fazla 10 MB olabilir.");
            var fileName = Path.GetFileName(upload.DosyaAdi ?? string.Empty);
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(fileName) || !AllowedExtensions.Contains(extension))
                throw new InvalidOperationException("PDF, XML, HTML, XLSX, CSV, ZIP, gorsel veya ses dosyasi yukleyin.");
        }

        private static string NormalizeContentType(string contentType, string extension)
        {
            if (!string.IsNullOrWhiteSpace(contentType))
                return contentType.Trim();
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".xml" => "application/xml",
                ".html" or ".htm" => "text/html",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".csv" => "text/csv",
                ".zip" => "application/zip",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".webm" => "audio/webm",
                ".ogg" => "audio/ogg",
                ".m4a" => "audio/mp4",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                _ => "application/octet-stream"
            };
        }

        private static string BuildCsv(IEnumerable<Kasa> records)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Tarih,Tip,Tutar,OdemeYontemi,Kalem,Aciklama");
            foreach (var record in records)
            {
                builder.AppendLine(string.Join(",", new[]
                {
                    Csv(record.Tarih.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    Csv(record.Tip),
                    Csv(record.Tutar.ToString(CultureInfo.InvariantCulture)),
                    Csv(record.OdemeYontemi),
                    Csv(record.Kalem ?? record.GiderTuru ?? string.Empty),
                    Csv(record.Aciklama ?? string.Empty)
                }));
            }
            return builder.ToString();
        }

        private static string Csv(string value)
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        private static string BuildHtml(string title, object summary)
        {
            var json = JsonSerializer.Serialize(summary);
            return $"""
<!doctype html>
<html lang="tr">
<head><meta charset="utf-8"><title>{title}</title></head>
<body>
<h1>{title}</h1>
<pre>{json}</pre>
</body>
</html>
""";
        }

        private static byte[] BuildExpenseDetailsPdf(string title, string businessName, IEnumerable<Kasa> records)
        {
            var expenses = records
                .Where(x => string.Equals(x.Tip, "Gider", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Tip, "Cikis", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var lines = new List<string>
            {
                "SYSTEMCEL HARCAMA DETAYLARI",
                title,
                $"Isletme: {businessName}",
                $"Toplam gider: {expenses.Sum(x => x.Tutar).ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))} TL",
                $"Kayit sayisi: {expenses.Count}",
                ""
            };
            lines.AddRange(expenses.Select(x =>
                $"{x.Tarih:dd.MM.yyyy} | {x.Tutar.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))} TL | {x.OdemeYontemi} | {x.Kalem ?? x.GiderTuru ?? "-"} | {x.Aciklama ?? "-"}"));
            if (expenses.Count == 0)
                lines.Add("Secilen donemde harcama kaydi bulunmuyor.");

            return BuildSimplePdf(lines);
        }

        private static byte[] BuildSimplePdf(IReadOnlyList<string> lines)
        {
            const int linesPerPage = 44;
            var pages = lines.Chunk(linesPerPage).ToList();
            var objects = new List<string>();
            var pageObjectIds = new List<int>();
            var contentObjectIds = new List<int>();
            var fontObjectId = 3;
            var nextObjectId = 4;
            foreach (var _ in pages)
            {
                pageObjectIds.Add(nextObjectId++);
                contentObjectIds.Add(nextObjectId++);
            }

            objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
            objects.Add($"<< /Type /Pages /Kids [{string.Join(" ", pageObjectIds.Select(x => $"{x} 0 R"))}] /Count {pages.Count} >>");
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            for (var index = 0; index < pages.Count; index++)
            {
                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 {fontObjectId} 0 R >> >> /Contents {contentObjectIds[index]} 0 R >>");
                var content = new StringBuilder("BT\n/F1 9 Tf\n42 800 Td\n");
                foreach (var line in pages[index])
                    content.Append('(').Append(PdfEscape(ToPdfAscii(line))).Append(") Tj\n0 -17 Td\n");
                content.Append("ET");
                var stream = content.ToString();
                objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream");
            }

            using var output = new MemoryStream();
            using var writer = new StreamWriter(output, Encoding.ASCII, 1024, leaveOpen: true) { NewLine = "\n" };
            writer.WriteLine("%PDF-1.4");
            writer.Flush();
            var offsets = new List<long> { 0 };
            for (var index = 0; index < objects.Count; index++)
            {
                offsets.Add(output.Position);
                writer.WriteLine($"{index + 1} 0 obj");
                writer.WriteLine(objects[index]);
                writer.WriteLine("endobj");
                writer.Flush();
            }
            var xref = output.Position;
            writer.WriteLine($"xref\n0 {objects.Count + 1}");
            writer.WriteLine("0000000000 65535 f ");
            foreach (var offset in offsets.Skip(1))
                writer.WriteLine($"{offset:0000000000} 00000 n ");
            writer.WriteLine($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
            writer.Flush();
            return output.ToArray();
        }

        private static string ToPdfAscii(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            return new string(normalized.Where(x => x <= 127 && CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark).ToArray())
                .Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal);
        }

        private static string PdfEscape(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("(", "\\(", StringComparison.Ordinal).Replace(")", "\\)", StringComparison.Ordinal);

        private static string DisplayName(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private sealed record DataPackage(string Title, string SummaryJson, string ZipPath, string PdfPath);

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

        private static readonly Regex SocialContactRegex = new(
            @"\b(?:instagram|whatsapp|telegram|linkedin|facebook|x\.com|twitter|tiktok|@)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }
}
