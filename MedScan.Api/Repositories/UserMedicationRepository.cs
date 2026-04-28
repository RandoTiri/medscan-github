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
            .AsNoTracking()
            .Where(x => x.ProfileId == profileId && x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => new UserMedication
            {
                Id = x.Id,
                ProfileId = x.ProfileId,
                MedicationId = x.MedicationId,
                Frequency = x.Frequency,
                ScheduleUnit = x.ScheduleUnit,
                ScheduledTimesJson = x.ScheduledTimesJson,
                WeeklyDaysJson = x.WeeklyDaysJson,
                StartDate = x.StartDate,
                ExpiresOn = x.ExpiresOn,
                RemindersEnabled = x.RemindersEnabled,
                Notes = x.Notes,
                AddedAt = x.AddedAt,
                IsActive = x.IsActive,
                Profile = new Profile
                {
                    Id = x.Profile.Id,
                    Name = x.Profile.Name
                },
                Medication = new Medication
                {
                    Id = x.Medication.Id,
                    Name = x.Medication.Name,
                    StrengthMg = x.Medication.StrengthMg
                },
                DoseLogs = x.DoseLogs
                    .Select(log => new DoseLog
                    {
                        Id = log.Id,
                        UserMedicationId = log.UserMedicationId,
                        ScheduledTime = log.ScheduledTime,
                        TakenAt = log.TakenAt,
                        DoseStatus = log.DoseStatus,
                        ConfirmedByUserId = log.ConfirmedByUserId
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<UserMedication?> GetByIdAsync(int userMedicationId) {
        return await _dbContext.UserMedications
            .AsNoTracking()
            .Where(x => x.Id == userMedicationId)
            .Select(x => new UserMedication
            {
                Id = x.Id,
                ProfileId = x.ProfileId,
                MedicationId = x.MedicationId,
                Frequency = x.Frequency,
                ScheduleUnit = x.ScheduleUnit,
                ScheduledTimesJson = x.ScheduledTimesJson,
                WeeklyDaysJson = x.WeeklyDaysJson,
                StartDate = x.StartDate,
                ExpiresOn = x.ExpiresOn,
                RemindersEnabled = x.RemindersEnabled,
                Notes = x.Notes,
                AddedAt = x.AddedAt,
                IsActive = x.IsActive,
                Profile = new Profile
                {
                    Id = x.Profile.Id,
                    Name = x.Profile.Name
                },
                Medication = new Medication
                {
                    Id = x.Medication.Id,
                    Name = x.Medication.Name,
                    StrengthMg = x.Medication.StrengthMg
                },
                DoseLogs = x.DoseLogs
                    .Select(log => new DoseLog
                    {
                        Id = log.Id,
                        UserMedicationId = log.UserMedicationId,
                        ScheduledTime = log.ScheduledTime,
                        TakenAt = log.TakenAt,
                        DoseStatus = log.DoseStatus,
                        ConfirmedByUserId = log.ConfirmedByUserId
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<UserMedication?> GetTrackedByIdAsync(int userMedicationId) {
        return await _dbContext.UserMedications
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
