using MedScan.Shared.Models;

namespace MedScan.Api.Repositories;

public interface IHomePharmacyRepository {
    Task<List<HomePharmacyItem>> GetByProfileIdAsync(int profileId);
    Task<HomePharmacyItem?> GetByIdAsync(int id);
    Task<HomePharmacyItem?> FindNewestTrackedByProfileAndMedicationAsync(int profileId,int medicationId);
    Task<DateOnly?> FindOldestExpiryForMedicationAsync(int medicationId,CancellationToken cancellationToken = default);
    Task<bool> IsOwnedByUserAsync(int id,string userId,CancellationToken cancellationToken = default);
    Task AddAsync(HomePharmacyItem item);
    void Remove(HomePharmacyItem item);
    Task SaveChangesAsync();
}