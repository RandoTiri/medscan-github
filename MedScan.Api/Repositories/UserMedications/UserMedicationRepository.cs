using MedScan.Api.Data;
using MedScan.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Repositories.UserMedications;

public sealed class UserMedicationRepository(AppDbContext dbContext) : IUserMedicationRepository {
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<List<UserMedication>> GetByProfileIdAsync(int profileId) {
        return await ProjectScheduleView(WithAvailableStock(_dbContext.UserMedications))
            .Where(x =>
                x.ProfileId == profileId &&
                x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync();
    }

    public async Task<UserMedication?> GetByIdAsync(int userMedicationId) {
        return await ProjectScheduleView(WithAvailableStock(_dbContext.UserMedications))
            .Where(x =>
                x.Id == userMedicationId &&
                x.IsActive)
            .FirstOrDefaultAsync();
    }

    public async Task<UserMedication?> GetByIdRawAsync(int userMedicationId) {
        return await ProjectScheduleView(_dbContext.UserMedications)
            .Where(x => x.Id == userMedicationId)
            .FirstOrDefaultAsync();
    }

    public async Task<UserMedication?> GetTrackedByIdAsync(int userMedicationId) {
        return await _dbContext.UserMedications
            .FirstOrDefaultAsync(x => x.Id == userMedicationId);
    }

    public Task<UserMedication?> GetTrackedByIdWithLogsAndMedicationAsync(int userMedicationId) {
        return _dbContext.UserMedications
            .Include(um => um.Medication)
            .Include(um => um.DoseLogs)
            .FirstOrDefaultAsync(um => um.Id == userMedicationId);
    }

    public Task<UserMedication?> GetNewestActiveTrackedByProfileAndMedicationAsync(int profileId,int medicationId) {
        return _dbContext.UserMedications
            .Where(um => um.ProfileId == profileId && um.MedicationId == medicationId && um.IsActive)
            .OrderByDescending(um => um.Id)
            .FirstOrDefaultAsync();
    }

    public Task<bool> IsOwnedByUserAsync(int userMedicationId,string userId,CancellationToken cancellationToken = default) {
        return _dbContext.UserMedications
            .AsNoTracking()
            .AnyAsync(m => m.Id == userMedicationId && m.Profile.UserId == userId,cancellationToken);
    }

    public async Task<List<UserMedication>> GetTrackedActiveByProfileAndMedicationAsync(int profileId,int medicationId) {
        return await _dbContext.UserMedications
            .Where(x =>
                x.ProfileId == profileId &&
                x.MedicationId == medicationId &&
                x.IsActive)
            .ToListAsync();
    }

    public async Task AddAsync(UserMedication userMedication) {
        await _dbContext.UserMedications.AddAsync(userMedication);
    }

    public Task SaveChangesAsync() {
        return _dbContext.SaveChangesAsync();
    }

    private IQueryable<UserMedication> ProjectScheduleView(IQueryable<UserMedication> query) {
        return query
            .AsNoTracking()
            .Select(x => new UserMedication {
                Id = x.Id,
                ProfileId = x.ProfileId,
                MedicationId = x.MedicationId,
                Frequency = x.Frequency,
                ScheduleUnit = x.ScheduleUnit,
                ScheduledTimesJson = x.ScheduledTimesJson,
                WeeklyDaysJson = x.WeeklyDaysJson,
                StartDate = x.StartDate,
                ExpiresOn = _dbContext.HomePharmacyItems
                    .Where(item =>
                        item.ProfileId == x.ProfileId &&
                        item.MedicationId == x.MedicationId &&
                        item.Quantity > 0)
                    .Min(item => item.ExpiresOn),
                RemindersEnabled = x.RemindersEnabled,
                Notes = x.Notes,
                AddedAt = x.AddedAt,
                IsActive = x.IsActive,
                Profile = new Profile {
                    Id = x.Profile.Id,
                    Name = x.Profile.Name
                },
                Medication = new Medication {
                    Id = x.Medication.Id,
                    Name = x.Medication.Name,
                    StrengthMg = x.Medication.StrengthMg
                },
                DoseLogs = x.DoseLogs
                    .Select(log => new DoseLog {
                        Id = log.Id,
                        UserMedicationId = log.UserMedicationId,
                        ScheduledTime = log.ScheduledTime,
                        TakenAt = log.TakenAt,
                        DoseStatus = log.DoseStatus,
                        ConfirmedByUserId = log.ConfirmedByUserId
                    })
                    .ToList()
            });
    }

    private IQueryable<UserMedication> WithAvailableStock(IQueryable<UserMedication> query) =>
        query.Where(userMedication =>
            _dbContext.HomePharmacyItems.Any(item =>
                item.ProfileId == userMedication.ProfileId &&
                item.MedicationId == userMedication.MedicationId &&
                item.Quantity > 0));
}