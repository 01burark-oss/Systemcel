using System.Linq;
using CashTracker.Core.Models;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class SubscriptionPlanCatalogTests
    {
        [Theory]
        [InlineData(0, 899)]
        [InlineData(1, 949)]
        [InlineData(12, 1499)]
        public void MuhasebeciStandartFiyati_SatinAlinanKrediyeGoreHesaplanir(int ekMusteriKredisi, decimal expected)
        {
            var actual = SubscriptionPlanCatalog.CalculateMuhasebeciStandartAylikTutar(ekMusteriKredisi);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(11, false)]
        [InlineData(12, true)]
        public void MuhasebeciProOnerisi_StandartKredileriProFiyatinaEsitleninceBaslar(int ekMusteriKredisi, bool expected)
        {
            var actual = SubscriptionPlanCatalog.ShouldRecommendMuhasebeciPro(ekMusteriKredisi);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MuhasebeciYillikPlanlari_YuzdeOnAltiAvantajlidir()
        {
            var standart = SubscriptionPlanCatalog.Plans.Single(x => x.Kod == PlanKodlari.MuhasebeciStandart);
            var pro = SubscriptionPlanCatalog.Plans.Single(x => x.Kod == PlanKodlari.MuhasebeciPro);

            Assert.Equal(standart.AylikTutar * 12 * 0.84m, standart.YillikTutar);
            Assert.Equal(pro.AylikTutar * 12 * 0.84m, pro.YillikTutar);
            Assert.Equal(9061.92m, standart.YillikTutar);
            Assert.Equal(15109.92m, pro.YillikTutar);
            Assert.Equal(7045.92m, standart.KurucuYillikTutar);
            Assert.Equal(12085.92m, pro.KurucuYillikTutar);
        }

        [Fact]
        public void BaslangicPlani_KararVerilenHaklariTasir()
        {
            var plan = SubscriptionPlanCatalog.Plans.Single(x => x.Kod == PlanKodlari.IsletmeBaslangic);

            Assert.Equal(690, plan.AylikTutar);
            Assert.Equal(6624, plan.YillikTutar);
            Assert.Equal(490, plan.KurucuAylikTutar);
            Assert.Equal(4704, plan.KurucuYillikTutar);
            Assert.Equal(100, plan.AiMesajLimiti);
            Assert.Equal(50, plan.FaturaLimiti);
            Assert.Equal(HesapTipleri.Isletme, plan.HesapTipi);
        }

        [Fact]
        public void ZipIsletmePlanlari_FiyatVeKurumsalHaklariTasir()
        {
            var buyume = SubscriptionPlanCatalog.Plans.Single(x => x.Kod == PlanKodlari.IsletmeBuyume);
            var kurumsal = SubscriptionPlanCatalog.Plans.Single(x => x.Kod == PlanKodlari.IsletmeKurumsal);

            Assert.Equal(1290, buyume.AylikTutar);
            Assert.Equal(12384, buyume.YillikTutar);
            Assert.Equal(990, buyume.KurucuAylikTutar);
            Assert.Equal(9504, buyume.KurucuYillikTutar);
            Assert.True(buyume.BankaMutabakatiAktif);
            Assert.True(buyume.MuhasebeciErisimiAktif);
            Assert.Equal(2490, kurumsal.AylikTutar);
            Assert.Equal(23904, kurumsal.YillikTutar);
            Assert.Equal(1990, kurumsal.KurucuAylikTutar);
            Assert.Equal(19104, kurumsal.KurucuYillikTutar);
            Assert.True(kurumsal.CokluSubeAktif);
            Assert.True(kurumsal.CokluParaBirimiAktif);
            Assert.True(kurumsal.ApiErisimiAktif);
            Assert.True(kurumsal.OncelikliDestekAktif);
            Assert.Null(kurumsal.KullaniciLimiti);
        }
    }
}
