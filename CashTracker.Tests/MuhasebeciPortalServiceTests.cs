using System;
using System.IO;
using System.Linq;
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
            Assert.Contains(durum.Sohbetler, x => x.MusteriIsletmeId == ids.CustomerId && x.HedefUrl.Contains("sohbet=1"));
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
