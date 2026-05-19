using MedScan.Api.Data;
using MedScan.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Repositories.DoseLogs;

public sealed class DoseLogRepository(AppDbContext dbContext) : IDoseLogRepository {
    private readonly AppDbContext _dbContext = dbContext;
    public void Add(DoseLog log) {
        _dbContext.DoseLogs.Add(log);
    }

    public Task<List<DoseLog>> GetByProfileInRangeAsync(int profileId,DateTime utcStart,DateTime utcEnd) {
        return _dbContext.DoseLogs
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