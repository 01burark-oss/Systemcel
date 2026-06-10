using System.Collections.Generic;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IIsletmeService
    {
        Task<List<Isletme>> GetAllAsync();
        Task<Isletme?> GetByIdAsync(int id);
        Task<Isletme> GetActiveAsync();
        Task<int> GetActiveIdAsync();
        Task<int> CreateAsync(string ad, bool makeActive = false);
        Task RenameAsync(int id, string ad);
        Task UpdateSetupAsync(int id, string ad, string isletmeTuru, string konum, bool tamamlandi, string? hesapTipi = null, bool? muhasebeciVarMi = null, MuhasebeciProfilKaydetRequest? muhasebeciProfil = null);
        Task SetActiveAsync(int id);
        Task SetActiveCustomerContextAsync(int musteriIsletmeId);
        Task ClearActiveCustomerContextAsync();
        Task<ActiveBusinessAccess> GetActiveAccessAsync();
        Task DeleteAsync(int id);
    }
}
