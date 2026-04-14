using MedScan.Api.Data;
using MedScan.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Repositories;

public sealed class HomePharmacyRepository(AppDbContext dbContext) : IHomePharmacyRepository
{
    public async Task<List<HomePharmacyItem>> GetByProfileIdAsync(int profileId)
    {
        return await dbContext.HomePharmacyItems
            .Include(x => x.Profile)
            .Include(x => x.Medication)
            .Where(x => x.ProfileId == profileId)
            .OrderByDescending(x => x.AddedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync();
    }

    public async Task<HomePharmacyItem?> GetByIdAsync(int id)
    {
        return await dbContext.HomePharmacyItems
            .Include(x => x.Profile)
            .Include(x => x.Medication)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<HomePharmacyItem?> GetByProfileAndMedicationAsync(int profileId, int medicationId)
    {
        return await dbContext.HomePharmacyItems
            .Include(x => x.Profile)
            .Include(x => x.Medication)
            .FirstOrDefaultAsync(x => x.ProfileId == profileId && x.MedicationId == medicationId);
    }

    public async Task AddAsync(HomePharmacyItem item)
    {
        await dbContext.HomePharmacyItems.AddAsync(item);
    }

    public void Remove(HomePharmacyItem item)
    {
        dbContext.HomePharmacyItems.Remove(item);
    }

    public Task SaveChangesAsync()
    {
        return dbContext.SaveChangesAsync();
    }
}
