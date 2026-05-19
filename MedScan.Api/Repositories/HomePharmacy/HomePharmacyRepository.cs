using MedScan.Api.Data;
using MedScan.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Repositories.HomePharmacy;

public sealed class HomePharmacyRepository(AppDbContext dbContext) : IHomePharmacyRepository {
    private readonly AppDbContext _dbContext = dbContext;
    public async Task<List<HomePharmacyItem>> GetByProfileIdAsync(int profileId) {
        return await _dbContext.HomePharmacyItems
            .Include(x => x.Profile)
            .Include(x => x.Medication)
            .Where(x => x.ProfileId == profileId)
            .OrderByDescending(x => x.AddedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync();
    }

    public async Task<HomePharmacyItem?> GetByIdAsync(int id) {
        return await _dbContext.HomePharmacyItems
            .Include(x => x.Profile)
            .Include(x => x.Medication)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<HomePharmacyItem?> FindNewestTrackedByProfileAndMedicationAsync(int profileId,int medicationId) {
        return _dbContext.HomePharmacyItems
            .Where(item => item.ProfileId == profileId && item.MedicationId == medicationId)
            .OrderByDescending(item => item.AddedAt)
            .FirstOrDefaultAsync();
    }

    public Task<DateOnly?> FindOldestExpiryForMedicationAsync(int medicationId,CancellationToken cancellationToken = default) {
        return _dbContext.HomePharmacyItems
            .AsNoTracking()
            .Where(x => x.MedicationId == medicationId && x.ExpiresOn != null)
            .OrderBy(x => x.ExpiresOn)
            .Select(x => x.ExpiresOn)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> IsOwnedByUserAsync(int id,string userId,CancellationToken cancellationToken = default) {
        return _dbContext.HomePharmacyItems
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && x.Profile.UserId == userId,cancellationToken);
    }

    public async Task AddAsync(HomePharmacyItem item) {
        await _dbContext.HomePharmacyItems.AddAsync(item);
    }

    public void Remove(HomePharmacyItem item) {
        _dbContext.HomePharmacyItems.Remove(item);
    }

    public Task SaveChangesAsync() {
        return _dbContext.SaveChangesAsync();
    }
}