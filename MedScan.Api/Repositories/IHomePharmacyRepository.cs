using MedScan.Shared.Models;

namespace MedScan.Api.Repositories;

public interface IHomePharmacyRepository
{
    Task<List<HomePharmacyItem>> GetByProfileIdAsync(int profileId);
    Task<HomePharmacyItem?> GetByIdAsync(int id);
    Task AddAsync(HomePharmacyItem item);
    void Remove(HomePharmacyItem item);
    Task SaveChangesAsync();
}
