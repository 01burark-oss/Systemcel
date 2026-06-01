using System.Linq;
using CashTracker.Core.Models;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class SubscriptionPlanCatalogTests
    {
        [Theory]
        [InlineData(0, 699)]
        [InlineData(10, 699)]
        [InlineData(11, 749)]
        [InlineData(20, 1199)]
        public void MuhasebeciStandartFiyati_MusteriSayisinaGoreHesaplanir(int musteriSayisi, decimal expected)
        {
            var actual = SubscriptionPlanCatalog.CalculateMuhasebeciStandartAylikTutar(musteriSayisi);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(19, false)]
        [InlineData(20, true)]
        public void MuhasebeciProOnerisi_StandartFiyatProyaEsitleninceBaslar(int musteriSayisi, bool expected)
        {
            var actual = SubscriptionPlanCatalog.ShouldRecommendMuhasebeciPro(musteriSayisi);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void BaslangicPlani_KararVerilenHaklariTasir()
        {
            var plan = SubscriptionPlanCatalog.Plans.Single(x => x.Kod == PlanKodlari.IsletmeBaslangic);

            Assert.Equal(199, plan.AylikTutar);
            Assert.Equal(50, plan.AiMesajLimiti);
            Assert.Equal(HesapTipleri.Isletme, plan.HesapTipi);
        }
    }
}
