using MedScan.Api.Data;
using MedScan.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Repositories;

public sealed class UserMedicationRepository : IUserMedicationRepository {
    private readonly AppDbContext _dbContext;

    public UserMedicationRepository(AppDbContext dbContext) {
        _dbContext = dbContext;
    }

    public async Task<List<UserMedication>> GetByProfileIdAsync(int profileId) {
        return await _dbContext.UserMedications
            .Include(x => x.Profile)
            .Include(x => x.Medication)
            .Include(x => x.DoseLogs)
            .Where(x => x.ProfileId == profileId && x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync();
    }

    public async Task<UserMedication?> GetByIdAsync(int userMedicationId) {
        return await _dbContext.UserMedications
            .Include(x => x.Profile)
            .Include(x => x.Medication)
            .Include(x => x.DoseLogs)
            .FirstOrDefaultAsync(x => x.Id == userMedicationId);
    }

    public async Task AddAsync(UserMedication userMedication) {
        await _dbContext.UserMedications.AddAsync(userMedication);
    }

    public void Remove(UserMedication userMedication) {
        _dbContext.UserMedications.Remove(userMedication);
    }

    public Task SaveChangesAsync() {
        return _dbContext.SaveChangesAsync();
    }
}
