using MedScan.Api.Data;
using MedScan.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Repositories;

public sealed class DoseLogRepository(AppDbContext dbContext) : IDoseLogRepository {
    public void Add(DoseLog log) {
        dbContext.DoseLogs.Add(log);
    }

    public Task<List<DoseLog>> GetByProfileInRangeAsync(int profileId,DateTime utcStart,DateTime utcEnd) {
        return dbContext.DoseLogs
            .AsNoTracking()
            .Include(log => log.UserMedication)
                .ThenInclude(um => um.Medication)
            .Where(log =>
                log.UserMedication.ProfileId == profileId &&
                log.ScheduledTime >= utcStart &&
                log.ScheduledTime < utcEnd)
            .OrderBy(log => log.ScheduledTime)
            .ToListAsync();
    }
}