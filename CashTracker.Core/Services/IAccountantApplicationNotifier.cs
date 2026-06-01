using System.Threading;
using System.Threading.Tasks;

namespace CashTracker.Core.Services
{
    public sealed record AccountantApplicationNotification(
        string AdSoyad,
        string Eposta,
        string IsletmeAdi,
        string IsletmeTuru,
        string Konum);

    public interface IAccountantApplicationNotifier
    {
        Task NotifyApplicationCreatedAsync(AccountantApplicationNotification notification, CancellationToken ct = default);
    }
}
