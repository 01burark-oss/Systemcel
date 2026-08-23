using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class MuhasebeciPortalServiceTests
    {
        [Fact]
        public async Task ProfilYayinlaninca_PublicPazaryerindeListelenir()
        {
            using var fixture = await MuhasebeciPortalFixture.CreateAsync();
            var ids = await fixture.CreateAccountantAndCustomerAsync();

            fixture.CurrentUser.Set("accountant", "accountant@example.com", "Ada Muhasebe");
            var profile = await fixture.Portal.SaveProfileAsync(new MuhasebeciProfilKaydetRequest
            {
                Yayinda = true,
                Unvan = "Ada Muhasebe",
                Konum = "Istanbul / Kadikoy",
                Telefon = "+90 532 000 00 00",
                DeneyimYili = 8,
                ProfilResmiUrl = "https://cdn.systemcel.test/ada.png",
                UcretBilgisi = "Aylik 2500 TL'den baslar",
                Uzmanliklar = "E-fatura, KOBI",
                MusteriTipleri = "Kafe ve perakende",
                KisaAciklama = "KOBI ekipleri icin donem takibi."
            });

            var marketplace = await fixture.Portal.GetPublicMarketplaceAsync("kobi");

            Assert.True(profile.Yayinda);
            Assert.Contains(marketplace.Profiller, x => x.MuhasebeciIsletmeId == ids.AccountantId && x.Unvan == "Ada Muhasebe");
            var listed = marketplace.Profiller.Single(x => x.MuhasebeciIsletmeId == ids.AccountantId);
            Assert.Equal(string.Empty, listed.Telefon);
            Assert.Equal(string.Empty, listed.PlanAdi);
        }

        [Fact]
        public async Task PazaryeriTalebi_KabulEdilinceAktifIliskiyeDonusur()
        {
            using var fixture = await MuhasebeciPortalFixture.CreateAsync();
            var ids = await fixture.CreateAccountantAndCustomerAsync();
            await fixture.PublishDefaultProfileAsync();

            fixture.CurrentUser.Set("customer", "customer@example.com", "Bahar Kafe");
            var talep = await fixture.Portal.SubmitMarketplaceRequestAsync(ids.AccountantId, new MuhasebeciTalepOlusturRequest
            {
                YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.OkumaRapor,
                Mesaj = "Defter kontrolu icin destek istiyoruz."
            });

            fixture.CurrentUser.Set("accountant", "accountant@example.com", "Ada Muhasebe");
            await fixture.Portal.AcceptRequestAsync(talep.Id, new MuhasebeciTalepKararRequest
            {
                YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.TamIslem
            });

            await using var db = fixture.CreateDbContext();
            var relation = await db.MuhasebeciMusterileri.SingleAsync(x =>
                x.MuhasebeciIsletmeId == ids.AccountantId &&
                x.MusteriIsletmeId == ids.CustomerId);
            var savedRequest = await db.MuhasebeciMusteriTalepleri.SingleAsync(x => x.Id == talep.Id);

            Assert.Equal("Aktif", relation.Durum);
            Assert.Equal(MuhasebeciYetkiSeviyeleri.TamIslem, relation.YetkiSeviyesi);
            Assert.Equal(MuhasebeciTalepDurumlari.Kabul, savedRequest.Durum);
            Assert.Equal(talep.Id, relation.TalepId);
        }

        [Fact]
        public async Task PazaryeriTalebi_DogrudanIletisimBilgisiIcerirseEngellenir()
        {
            using var fixture = await MuhasebeciPortalFixture.CreateAsync();
            var ids = await fixture.CreateAccountantAndCustomerAsync();
            await fixture.PublishDefaultProfileAsync();

            fixture.CurrentUser.Set("customer", "customer@example.com", "Bahar Kafe");
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.Portal.SubmitMarketplaceRequestAsync(ids.AccountantId, new MuhasebeciTalepOlusturRequest
                {
                    YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.OkumaRapor,
                    Mesaj = "Beni +90 532 000 00 00 numarasından arayın."
                }));
            var fragmentedError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.Portal.SubmitMarketplaceRequestAsync(ids.AccountantId, new MuhasebeciTalepOlusturRequest
                {
                    YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.OkumaRapor,
                    Mesaj = "Bana 0530 merhaba 065 merhaba 58 merhaba 88 üzerinden ulaşın."
                }));

            Assert.Contains("paylaşılamaz", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("paylaşılamaz", fragmentedError.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Sohbet_BildirimDurumuOkunmamisMesajiGosterirVeAcilincaTemizler()
        {
            using var fixture = await MuhasebeciPortalFixture.CreateAsync();
            var ids = await fixture.CreateAccountantAndCustomerAsync();
            await fixture.PublishDefaultProfileAsync();

            fixture.CurrentUser.Set("customer", "customer@example.com", "Bahar Kafe");
            var talep = await fixture.Portal.SubmitMarketplaceRequestAsync(ids.AccountantId, new MuhasebeciTalepOlusturRequest
            {
                YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.OkumaRapor,
                Mesaj = "Aylik raporlama icin goruselim."
            });
            await fixture.Portal.SendCustomerConversationMessageAsync(ids.AccountantId, new MuhasebeciSohbetMesajiGonderRequest
            {
                Mesaj = "Belgeleri Systemcel uzerinden paylastim."
            });

            fixture.CurrentUser.Set("accountant", "accountant@example.com", "Ada Muhasebe");
            var durum = await fixture.Portal.GetConversationNotificationStatusAsync();
            await fixture.Portal.GetAccountantRequestConversationAsync(talep.Id);
            var temizDurum = await fixture.Portal.GetConversationNotificationStatusAsync();

            Assert.Equal(1, durum.OkunmamisMesajSayisi);
            Assert.Contains(durum.Sohbetler, x => x.MusteriIsletmeId == ids.CustomerId && x.HedefUrl.Contains("/app/sohbetler"));
            Assert.Equal(0, temizDurum.OkunmamisMesajSayisi);
            Assert.Contains(temizDurum.Sohbetler, x => x.MusteriIsletmeId == ids.CustomerId && x.OkunmamisMesajSayisi == 0);
        }

        [Fact]
        public async Task Sohbet_UygulamaIcindeMesajlasirVeTelefonuEngeller()
        {
            using var fixture = await MuhasebeciPortalFixture.CreateAsync();
            var ids = await fixture.CreateAccountantAndCustomerAsync();
            await fixture.PublishDefaultProfileAsync();

            fixture.CurrentUser.Set("customer", "customer@example.com", "Bahar Kafe");
            await fixture.Portal.SubmitMarketplaceRequestAsync(ids.AccountantId, new MuhasebeciTalepOlusturRequest
            {
                YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.OkumaRapor,
                Mesaj = "Aylik raporlama icin goruselim."
            });

            var sohbet = await fixture.Portal.SendCustomerConversationMessageAsync(ids.AccountantId, new MuhasebeciSohbetMesajiGonderRequest
            {
                Mesaj = "Belgeleri Systemcel uzerinden paylasabiliriz."
            });
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.Portal.SendCustomerConversationMessageAsync(ids.AccountantId, new MuhasebeciSohbetMesajiGonderRequest
                {
                    Mesaj = "Telefonum +90 532 000 00 00."
                }));
            var fragmentedError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.Portal.SendCustomerConversationMessageAsync(ids.AccountantId, new MuhasebeciSohbetMesajiGonderRequest
                {
                    Mesaj = "Bana ulasabilir misiniz? 0530 merhaba 065 merhaba 58 merhaba 88"
                }));

            Assert.Contains(sohbet.Mesajlar, x => x.BenimMesajim && x.Mesaj.Contains("Systemcel"));
            Assert.Contains("paylaşılamaz", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("paylaşılamaz", fragmentedError.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DavetKodu_KabulEdilinceMusteriBaglantisiOlusur()
        {
            using var fixture = await MuhasebeciPortalFixture.CreateAsync();
            var ids = await fixture.CreateAccountantAndCustomerAsync();

            fixture.CurrentUser.Set("accountant", "accountant@example.com", "Ada Muhasebe");
            var davet = await fixture.Portal.CreateInviteAsync(new MuhasebeciTalepOlusturRequest
            {
                YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.OkumaRapor,
                Mesaj = "Aylik raporlama icin baglanalim."
            }, "https://systemcel.test");

            fixture.CurrentUser.Set("customer", "customer@example.com", "Bahar Kafe");
            await fixture.Portal.AcceptInviteAsync(new MuhasebeciDavetKabulRequest
            {
                DavetKodu = davet.DavetKodu,
                YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.OkumaRapor
            });

            await using var db = fixture.CreateDbContext();
            var relation = await db.MuhasebeciMusterileri.SingleAsync(x =>
                x.MuhasebeciIsletmeId == ids.AccountantId &&
                x.MusteriIsletmeId == ids.CustomerId);

            Assert.Equal("Aktif", relation.Durum);
            Assert.Equal(MuhasebeciTalepTurleri.Davet, relation.Kaynak);
            Assert.Equal(davet.DavetKodu, relation.DavetKodu);
        }

        [Fact]
        public async Task IsletmeDavetLinki_MuhasebeciKabulEdinceSecilenYetkiyleBaglanir()
        {
            using var fixture = await MuhasebeciPortalFixture.CreateAsync();
            var ids = await fixture.CreateAccountantAndCustomerAsync();

            fixture.CurrentUser.Set("customer", "customer@example.com", "Bahar Kafe");
            var davet = await fixture.Portal.CreateCustomerLinkInviteAsync(
                new MuhasebeciLinkDavetOlusturRequest
                {
                    YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.TamIslem,
                    Mesaj = "Aylik belgelerimizi birlikte yonetelim."
                },
                "https://systemcel.test");
            var token = new Uri(davet.DavetLinki).Segments.Last().Trim('/');

            await using (var db = fixture.CreateDbContext())
            {
                var kayit = await db.MuhasebeciBaglantiDavetleri.SingleAsync();
                Assert.NotEqual(token, kayit.TokenHash);
                Assert.Equal(MuhasebeciTalepDurumlari.Beklemede, kayit.Durum);
                Assert.InRange(kayit.SonGecerlilikAt, DateTime.Now.AddDays(13), DateTime.Now.AddDays(15));
            }

            fixture.CurrentUser.Set("accountant", "accountant@example.com", "Ada Muhasebe");
            await fixture.Portal.AcceptCustomerLinkInviteAsync(new MuhasebeciLinkDavetKabulRequest
            {
                Token = token
            });

            await using (var db = fixture.CreateDbContext())
            {
                var relation = await db.MuhasebeciMusterileri.SingleAsync(x =>
                    x.MuhasebeciIsletmeId == ids.AccountantId &&
                    x.MusteriIsletmeId == ids.CustomerId);
                var kayit = await db.MuhasebeciBaglantiDavetleri.SingleAsync();

                Assert.Equal(MuhasebeciYetkiSeviyeleri.TamIslem, relation.YetkiSeviyesi);
                Assert.Equal(MuhasebeciTalepTurleri.MusteriDaveti, relation.Kaynak);
                Assert.Equal(MuhasebeciTalepDurumlari.Kabul, kayit.Durum);
                Assert.Equal(ids.AccountantId, kayit.MuhasebeciIsletmeId);
            }
        }

        [Fact]
        public async Task IsletmeDavetLinki_OnaysizMuhasebeciTarafindanKabulEdilemez()
        {
            using var fixture = await MuhasebeciPortalFixture.CreateAsync();
            var ids = await fixture.CreateAccountantAndCustomerAsync();

            fixture.CurrentUser.Set("customer", "customer@example.com", "Bahar Kafe");
            var davet = await fixture.Portal.CreateCustomerLinkInviteAsync(
                new MuhasebeciLinkDavetOlusturRequest(),
                "https://systemcel.test");
            var token = new Uri(davet.DavetLinki).Segments.Last().Trim('/');

            await using (var db = fixture.CreateDbContext())
            {
                var accountantUser = await db.Kullanicilar.SingleAsync(x => x.AuthProviderUserId == "accountant");
                accountantUser.Durum = KullaniciDurumlari.MuhasebeciOnayBekliyor;
                await db.SaveChangesAsync();
            }

            fixture.CurrentUser.Set("accountant", "accountant@example.com", "Ada Muhasebe");
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.Portal.AcceptCustomerLinkInviteAsync(new MuhasebeciLinkDavetKabulRequest { Token = token }));

            await using var verifyDb = fixture.CreateDbContext();
            Assert.Contains("onay bekliyor", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(await verifyDb.MuhasebeciMusterileri.AnyAsync(x =>
                x.MuhasebeciIsletmeId == ids.AccountantId && x.MusteriIsletmeId == ids.CustomerId));
        }

        [Fact]
        public async Task StandartPlan_EkMusteriKredisiKadarDavetKapasitesiVerir()
        {
            using var fixture = await MuhasebeciPortalFixture.CreateAsync();
            var ids = await fixture.CreateAccountantAndCustomerAsync();
            var now = DateTime.UtcNow;
            await using (var db = fixture.CreateDbContext())
            {
                db.Abonelikler.Add(new Abonelik
                {
                    IsletmeId = ids.AccountantId,
                    HesapTipi = HesapTipleri.Muhasebeci,
                    PlanKodu = PlanKodlari.MuhasebeciStandart,
                    Durum = "Aktif",
                    FaturalamaDonemi = PaymentBillingPeriods.Monthly,
                    EkMusteriKredisi = 2,
                    AylikTutar = 799m,
                    DonemTutari = 799m,
                    DonemBaslangicAt = now.AddDays(-1),
                    DonemBitisAt = now.AddMonths(1),
                    CreatedAt = now.AddDays(-1),
                    UpdatedAt = now
                });

                for (var index = 0; index < 12; index++)
                {
                    var customer = new Isletme
                    {
                        Ad = $"Limit müşteri {index + 1}",
                        TenantTipi = HesapTipleri.Isletme,
                        IsAktif = false,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    db.Isletmeler.Add(customer);
                    await db.SaveChangesAsync();
                    db.MuhasebeciMusterileri.Add(new MuhasebeciMusteri
                    {
                        MuhasebeciIsletmeId = ids.AccountantId,
                        MusteriIsletmeId = customer.Id,
                        Durum = "Aktif",
                        YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.OkumaRapor,
                        BaslangicAt = now,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
                await db.SaveChangesAsync();
            }

            fixture.CurrentUser.Set("accountant", "accountant@example.com", "Ada Muhasebe");
            var factory = new SingleDbContextFactory(fixture.Options);
            var portal = new MuhasebeciPortalService(
                factory,
                fixture.CurrentUser,
                fixture.IsletmeService,
                fixture.EntitlementService,
                new EntitlementGuard(fixture.EntitlementService));

            var error = await Assert.ThrowsAsync<EntitlementViolationException>(() => portal.CreateInviteAsync(
                new MuhasebeciTalepOlusturRequest
                {
                    YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.OkumaRapor,
                    Mesaj = "Kapasite kontrolü"
                },
                "https://systemcel.test"));

            Assert.Equal(EntitlementLimits.AccountantCustomer, error.LimitName);
            Assert.Equal(12, error.Limit);
            Assert.Equal(12, error.Current);
            Assert.Equal(PlanKodlari.MuhasebeciPro, error.SuggestedPlanCode);
        }

        [Fact]
        public async Task ProPlan_BelgeSagliginiYalnizAktifBagliMusteriyeAktarir()
        {
            using var fixture = await MuhasebeciPortalFixture.CreateAsync();
            var ids = await fixture.CreateAccountantAndCustomerAsync();
            await fixture.CreateRelationAsync(ids.AccountantId, ids.CustomerId, MuhasebeciYetkiSeviyeleri.OkumaRapor);
            var excludedIds = await fixture.CreateExcludedRelationsAsync(ids.AccountantId);
            await fixture.AddAccountantSubscriptionAsync(ids.AccountantId, PlanKodlari.MuhasebeciPro);
            var summary = new BelgeSaglikOzeti
            {
                Skor = 84,
                Durum = BelgeSaglikDurumlari.Dikkat
            };
            var belgeSaglik = new RecordingBelgeSaglikService(summary);
            var portal = fixture.CreatePortal(belgeSaglik);

            fixture.CurrentUser.Set("accountant", "accountant@example.com", "Ada Muhasebe");
            var panel = await portal.GetPanelAsync();

            Assert.True(panel.Entitlement!.MusteriSaglikSkoruAktif);
            var customer = Assert.Single(panel.Musteriler);
            Assert.Equal(ids.CustomerId, customer.IsletmeId);
            Assert.Same(summary, customer.BelgeSagligi);
            Assert.Equal(84, customer.BelgeSagligi!.Skor);
            var call = Assert.Single(belgeSaglik.Calls);
            Assert.Equal(ids.CustomerId, call.IsletmeId);
            Assert.Equal(DateTime.Today, call.ReferenceDate!.Value);
            Assert.DoesNotContain(belgeSaglik.Calls, x => x.IsletmeId == excludedIds.InactiveCustomerId);
            Assert.DoesNotContain(belgeSaglik.Calls, x => x.IsletmeId == excludedIds.OtherAccountantCustomerId);
        }

        [Fact]
        public async Task StandartPlan_BelgeSagliginiHesaplamazVeDtoAlaniniBosBirakir()
        {
            using var fixture = await MuhasebeciPortalFixture.CreateAsync();
            var ids = await fixture.CreateAccountantAndCustomerAsync();
            await fixture.CreateRelationAsync(ids.AccountantId, ids.CustomerId, MuhasebeciYetkiSeviyeleri.OkumaRapor);
            await fixture.AddAccountantSubscriptionAsync(ids.AccountantId, PlanKodlari.MuhasebeciStandart);
            var belgeSaglik = new RecordingBelgeSaglikService(new BelgeSaglikOzeti())
            {
                FailWhenCalled = true
            };
            var portal = fixture.CreatePortal(belgeSaglik);

            fixture.CurrentUser.Set("accountant", "accountant@example.com", "Ada Muhasebe");
            var panel = await portal.GetPanelAsync();

            Assert.False(panel.Entitlement!.MusteriSaglikSkoruAktif);
            Assert.Null(Assert.Single(panel.Musteriler).BelgeSagligi);
            Assert.Empty(belgeSaglik.Calls);
        }

        [Fact]
        public async Task MusteriBaglami_YetkiSeviyesineGoreYazmaHakkiDondurur()
        {
            using var fixture = await MuhasebeciPortalFixture.CreateAsync();
            var ids = await fixture.CreateAccountantAndCustomerAsync();
            await fixture.CreateRelationAsync(ids.AccountantId, ids.CustomerId, MuhasebeciYetkiSeviyeleri.OkumaRapor);

            fixture.CurrentUser.Set("accountant", "accountant@example.com", "Ada Muhasebe");
            await fixture.Portal.OpenCustomerContextAsync(ids.CustomerId);
            var readOnlyAccess = await fixture.IsletmeService.GetActiveAccessAsync();
            var activeCustomer = await fixture.IsletmeService.GetActiveAsync();

            Assert.True(readOnlyAccess.MuhasebeciMusteriBaglami);
            Assert.False(readOnlyAccess.YazmaYetkisi);
            Assert.Equal(ids.CustomerId, readOnlyAccess.IsletmeId);
            Assert.Equal(ids.CustomerId, activeCustomer.Id);

            await fixture.Portal.CloseCustomerContextAsync();
            await fixture.SetRelationPermissionAsync(ids.AccountantId, ids.CustomerId, MuhasebeciYetkiSeviyeleri.TamIslem);
            await fixture.Portal.OpenCustomerContextAsync(ids.CustomerId);
            var fullAccess = await fixture.IsletmeService.GetActiveAccessAsync();

            Assert.True(fullAccess.YazmaYetkisi);
            Assert.Equal(MuhasebeciYetkiSeviyeleri.TamIslem, fullAccess.YetkiSeviyesi);
        }

        [Fact]
        public async Task MuhasebeciBasvurusu_OnaylanmadanPanelHazirOlmaz()
        {
            using var fixture = await MuhasebeciPortalFixture.CreateAsync();

            fixture.CurrentUser.Set("pending-accountant", "pending@example.com", "Bekleyen Muhasebe");
            var accountant = await fixture.IsletmeService.GetActiveAsync();
            await fixture.IsletmeService.UpdateSetupAsync(
                accountant.Id,
                "Bekleyen Muhasebe",
                "Muhasebe",
                "Ankara / Cankaya",
                true,
                HesapTipleri.Muhasebeci,
                muhasebeciProfil: BasvuruProfili("Bekleyen Muhasebe", "Ankara / Cankaya"));

            var panel = await fixture.Portal.GetPanelAsync();
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.Portal.SaveProfileAsync(new MuhasebeciProfilKaydetRequest
                {
                    Yayinda = true,
                    Unvan = "Bekleyen Muhasebe",
                    Konum = "Ankara / Cankaya",
                    Telefon = "+90 312 000 00 00",
                    DeneyimYili = 5,
                    ProfilResmiUrl = "https://cdn.systemcel.test/bekleyen.png",
                    UcretBilgisi = "Aylik 2000 TL'den baslar",
                    Uzmanliklar = "KOBI",
                    MusteriTipleri = "Hizmet",
                    KisaAciklama = "Basvuru onayi bekleniyor."
                }));

            await using var db = fixture.CreateDbContext();
            var user = await db.Kullanicilar.SingleAsync(x => x.AuthProviderUserId == "pending-accountant");

            Assert.False(panel.Hazir);
            Assert.Equal(KullaniciDurumlari.MuhasebeciOnayBekliyor, user.Durum);
            Assert.Contains("onay", panel.Mesaj, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("onay", error.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task MuhasebeciBasvurusu_YoneticiTarafindanOnaylanir()
        {
            using var fixture = await MuhasebeciPortalFixture.CreateAsync();

            fixture.CurrentUser.Set("pending-accountant", "pending@example.com", "Bekleyen Muhasebe");
            var accountant = await fixture.IsletmeService.GetActiveAsync();
            await fixture.IsletmeService.UpdateSetupAsync(
                accountant.Id,
                "Bekleyen Muhasebe",
                "Muhasebe",
                "Ankara / Cankaya",
                true,
                HesapTipleri.Muhasebeci,
                muhasebeciProfil: BasvuruProfili("Bekleyen Muhasebe", "Ankara / Cankaya"));

            var adminService = fixture.CreateYonetimService("admin-user");
            fixture.CurrentUser.Set("admin-user", "admin@example.com", "Admin");
            var liste = await adminService.GetMuhasebeciBasvurulariAsync("bekleyen");
            var onaylanan = await adminService.ApproveMuhasebeciBasvurusuAsync(liste.Basvurular.Single().KullaniciId);
            var marketplace = await fixture.Portal.GetPublicMarketplaceAsync("Bekleyen");

            fixture.CurrentUser.Set("pending-accountant", "pending@example.com", "Bekleyen Muhasebe");
            var panel = await fixture.Portal.GetPanelAsync();

            Assert.Equal(KullaniciDurumlari.Aktif, onaylanan.Durum);
            Assert.True(panel.Hazir);
            Assert.Contains(marketplace.Profiller, x => x.MuhasebeciIsletmeId == accountant.Id && x.Unvan == "Bekleyen Muhasebe");
        }

        private static MuhasebeciProfilKaydetRequest BasvuruProfili(string unvan, string konum)
        {
            return new MuhasebeciProfilKaydetRequest
            {
                Yayinda = false,
                Unvan = unvan,
                Konum = konum,
                Telefon = "+90 532 111 22 33",
                DeneyimYili = 6,
                ProfilResmiUrl = "https://cdn.systemcel.test/profil.png",
                UcretBilgisi = "Aylik 2500 TL'den baslar",
                Uzmanliklar = "KOBI, e-fatura",
                MusteriTipleri = "Kafe ve perakende",
                KisaAciklama = "Kucuk isletmeler icin aylik raporlama."
            };
        }

        [Fact]
        public async Task MuhasebeciBasvurulari_YoneticiOlmayanKullaniciyaKapali()
        {
            using var fixture = await MuhasebeciPortalFixture.CreateAsync();
            var adminService = fixture.CreateYonetimService("admin-user");

            fixture.CurrentUser.Set("regular-user", "regular@example.com", "Regular User");

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                adminService.GetMuhasebeciBasvurulariAsync("bekleyen"));
        }

        private sealed class MutableCurrentUserContext : ICurrentUserContext
        {
            private CurrentUserIdentity? _current;

            public void Set(string providerUserId, string email, string fullName)
            {
                _current = new CurrentUserIdentity(providerUserId, email, fullName);
            }

            public CurrentUserIdentity? GetCurrentUser()
            {
                return _current;
            }
        }

        private sealed class RecordingBelgeSaglikService : IBelgeSaglikService
        {
            private readonly object _sync = new();
            private readonly BelgeSaglikOzeti _summary;
            private readonly List<(int IsletmeId, DateTime? ReferenceDate)> _calls = new();

            public RecordingBelgeSaglikService(BelgeSaglikOzeti summary)
            {
                _summary = summary;
            }

            public bool FailWhenCalled { get; init; }

            public IReadOnlyList<(int IsletmeId, DateTime? ReferenceDate)> Calls
            {
                get
                {
                    lock (_sync)
                        return _calls.ToList();
                }
            }

            public Task<BelgeSaglikOzeti> GetAsync(
                int isletmeId,
                DateTime? referenceDate = null,
                CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                lock (_sync)
                    _calls.Add((isletmeId, referenceDate));

                if (FailWhenCalled)
                    throw new InvalidOperationException("Belge sagligi Standart plan icin cagrilmamali.");

                return Task.FromResult(_summary);
            }
        }

        private sealed class MuhasebeciPortalFixture : IDisposable
        {
            private MuhasebeciPortalFixture(string dbPath, DbContextOptions<CashTrackerDbContext> options)
            {
                DbPath = dbPath;
                Options = options;
                CurrentUser = new MutableCurrentUserContext();
                var factory = new SingleDbContextFactory(options);
                IsletmeService = new IsletmeService(factory, CurrentUser);
                EntitlementService = new SubscriptionEntitlementService(factory);
                Portal = new MuhasebeciPortalService(factory, CurrentUser, IsletmeService, EntitlementService);
            }

            public string DbPath { get; }
            public DbContextOptions<CashTrackerDbContext> Options { get; }
            public MutableCurrentUserContext CurrentUser { get; }
            public IsletmeService IsletmeService { get; }
            public SubscriptionEntitlementService EntitlementService { get; }
            public MuhasebeciPortalService Portal { get; }
            public MuhasebeciPortalService CreatePortal(IBelgeSaglikService belgeSaglikService)
            {
                return new MuhasebeciPortalService(
                    new SingleDbContextFactory(Options),
                    CurrentUser,
                    IsletmeService,
                    EntitlementService,
                    belgeSaglikService: belgeSaglikService);
            }

            public SystemcelYonetimService CreateYonetimService(string adminClerkUserIds = "")
            {
                return new SystemcelYonetimService(
                    new SingleDbContextFactory(Options),
                    CurrentUser,
                    new SystemcelYonetimOptions { AdminClerkUserIds = adminClerkUserIds });
            }

            public static async Task<MuhasebeciPortalFixture> CreateAsync()
            {
                var dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_muhasebeci_portal_{Guid.NewGuid():N}.db");
                var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;
                var fixture = new MuhasebeciPortalFixture(dbPath, options);

                await using var db = fixture.CreateDbContext();
                await db.Database.EnsureCreatedAsync();
                return fixture;
            }

            public CashTrackerDbContext CreateDbContext()
            {
                return new CashTrackerDbContext(Options);
            }

            public async Task<(int AccountantId, int CustomerId)> CreateAccountantAndCustomerAsync()
            {
                CurrentUser.Set("accountant", "accountant@example.com", "Ada Muhasebe");
                var accountant = await IsletmeService.GetActiveAsync();
                await IsletmeService.UpdateSetupAsync(
                    accountant.Id,
                    "Ada Muhasebe",
                    "Muhasebe",
                    "Istanbul / Kadikoy",
                    true,
                    HesapTipleri.Muhasebeci,
                    muhasebeciProfil: BasvuruProfili("Ada Muhasebe", "Istanbul / Kadikoy"));
                await ApproveAccountantAsync("accountant");

                CurrentUser.Set("customer", "customer@example.com", "Bahar Kafe");
                var customer = await IsletmeService.GetActiveAsync();
                await IsletmeService.UpdateSetupAsync(
                    customer.Id,
                    "Bahar Kafe",
                    "Kafe",
                    "Izmir / Konak",
                    true,
                    HesapTipleri.Isletme);

                return (accountant.Id, customer.Id);
            }

            public async Task PublishDefaultProfileAsync()
            {
                CurrentUser.Set("accountant", "accountant@example.com", "Ada Muhasebe");
                await Portal.SaveProfileAsync(new MuhasebeciProfilKaydetRequest
                {
                    Yayinda = true,
                    Unvan = "Ada Muhasebe",
                    Konum = "Istanbul / Kadikoy",
                    Telefon = "+90 532 000 00 00",
                    DeneyimYili = 8,
                    ProfilResmiUrl = "https://cdn.systemcel.test/ada.png",
                    UcretBilgisi = "Aylik 2500 TL'den baslar",
                    Uzmanliklar = "KOBI, e-fatura",
                    MusteriTipleri = "Kafe ve perakende",
                    KisaAciklama = "Kucuk isletmeler icin aylik raporlama."
                });
            }

            private async Task ApproveAccountantAsync(string providerUserId)
            {
                await using var db = CreateDbContext();
                var user = await db.Kullanicilar.SingleAsync(x => x.AuthProviderUserId == providerUserId);
                user.Durum = KullaniciDurumlari.Aktif;
                user.UpdatedAt = DateTime.Now;
                await db.SaveChangesAsync();
            }

            public async Task CreateRelationAsync(int accountantId, int customerId, string permission)
            {
                await using var db = CreateDbContext();
                db.MuhasebeciMusterileri.Add(new MuhasebeciMusteri
                {
                    MuhasebeciIsletmeId = accountantId,
                    MusteriIsletmeId = customerId,
                    Durum = "Aktif",
                    YetkiSeviyesi = permission,
                    Kaynak = MuhasebeciTalepTurleri.Davet,
                    BaslangicAt = DateTime.Now.AddDays(-1),
                    KabulAt = DateTime.Now.AddDays(-1),
                    CreatedAt = DateTime.Now.AddDays(-1),
                    UpdatedAt = DateTime.Now.AddDays(-1)
                });
                await db.SaveChangesAsync();
            }

            public async Task<(int InactiveCustomerId, int OtherAccountantCustomerId)> CreateExcludedRelationsAsync(int accountantId)
            {
                await using var db = CreateDbContext();
                var now = DateTime.UtcNow;
                var inactiveCustomer = new Isletme
                {
                    Ad = "Pasif baglanti musterisi",
                    TenantTipi = HesapTipleri.Isletme,
                    IsAktif = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                var otherAccountant = new Isletme
                {
                    Ad = "Diger muhasebeci",
                    TenantTipi = HesapTipleri.Muhasebeci,
                    IsAktif = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                var otherCustomer = new Isletme
                {
                    Ad = "Diger muhasebecinin musterisi",
                    TenantTipi = HesapTipleri.Isletme,
                    IsAktif = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Isletmeler.AddRange(inactiveCustomer, otherAccountant, otherCustomer);
                await db.SaveChangesAsync();

                db.MuhasebeciMusterileri.AddRange(
                    new MuhasebeciMusteri
                    {
                        MuhasebeciIsletmeId = accountantId,
                        MusteriIsletmeId = inactiveCustomer.Id,
                        Durum = "Pasif",
                        YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.OkumaRapor,
                        BaslangicAt = now,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new MuhasebeciMusteri
                    {
                        MuhasebeciIsletmeId = otherAccountant.Id,
                        MusteriIsletmeId = otherCustomer.Id,
                        Durum = "Aktif",
                        YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.OkumaRapor,
                        BaslangicAt = now,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                await db.SaveChangesAsync();

                return (inactiveCustomer.Id, otherCustomer.Id);
            }

            public async Task AddAccountantSubscriptionAsync(int accountantId, string planCode)
            {
                await using var db = CreateDbContext();
                var now = DateTime.UtcNow;
                db.Abonelikler.Add(new Abonelik
                {
                    IsletmeId = accountantId,
                    HesapTipi = HesapTipleri.Muhasebeci,
                    PlanKodu = planCode,
                    Durum = "Aktif",
                    FaturalamaDonemi = PaymentBillingPeriods.Monthly,
                    AylikTutar = 1m,
                    DonemTutari = 1m,
                    DonemBaslangicAt = now.AddDays(-1),
                    DonemBitisAt = now.AddMonths(1),
                    CreatedAt = now.AddDays(-1),
                    UpdatedAt = now
                });
                await db.SaveChangesAsync();
            }

            public async Task SetRelationPermissionAsync(int accountantId, int customerId, string permission)
            {
                await using var db = CreateDbContext();
                var relation = await db.MuhasebeciMusterileri.SingleAsync(x =>
                    x.MuhasebeciIsletmeId == accountantId &&
                    x.MusteriIsletmeId == customerId);
                relation.YetkiSeviyesi = permission;
                relation.UpdatedAt = DateTime.Now;
                await db.SaveChangesAsync();
            }

            public void Dispose()
            {
                try
                {
                    if (File.Exists(DbPath))
                        File.Delete(DbPath);
                }
                catch
                {
                }
            }
        }
    }
}
