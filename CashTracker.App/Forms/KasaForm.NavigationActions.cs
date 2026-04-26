using System.Threading.Tasks;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        // Frontend butonu eklendiginde bu metod dogrudan cagrilacak.
        private async Task OpenSettingsForKalemManagementAsync()
        {
            if (EmbeddedSettingsRequested is not null)
            {
                EmbeddedSettingsRequested();
                return;
            }

            using var form = new SettingsForm(
                _isletmeService,
                _kalemTanimiService,
                _telegramApprovalService,
                _runtimeOptions,
                _appSecurityService,
                _licenseService,
                _receiptOcrSettings);
            form.ShowDialog(this);

            await LoadKalemlerForTipAsync();
            await LoadAllAsync();
        }
    }
}
