using CashTracker.Core.Models;
using Systemcel.Api.Api;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class ScreenApiNotificationTests
    {
        [Fact]
        public void PazaryeriTalebi_MuhasebecininBildirimKutusunaEklenir()
        {
            var notifications = ScreenApi.BuildAccountantRequestNotifications(new MuhasebeciPanelDto
            {
                Hazir = true,
                BekleyenTalepler = new List<MuhasebeciTalepDto>
                {
                    new()
                    {
                        Id = 42,
                        MusteriAdi = "Bahar Kafe",
                        Mesaj = "Aylık raporlama için görüşelim.",
                        Durum = MuhasebeciTalepDurumlari.Beklemede
                    }
                }
            });

            var notification = Assert.Single(notifications);
            Assert.Equal("muhasebeci-talep-42", notification.id);
            Assert.Equal("Bahar Kafe müşteri talebi gönderdi", notification.baslik);
            Assert.Equal("Aylık raporlama için görüşelim.", notification.mesaj);
            Assert.Equal("Talebi incele", notification.aksiyon);
            Assert.Equal("/app/muhasebeci?talepId=42&sohbet=1", notification.url);
        }
    }
}
