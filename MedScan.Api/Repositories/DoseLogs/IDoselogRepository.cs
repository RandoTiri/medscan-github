using MedScan.Shared.Models;

namespace MedScan.Api.Repositories.DoseLogs;

public interface IDoseLogRepository {
    void Add(DoseLog log);
    Task<List<DoseLog>> GetByProfileInRangeAsync(int profileId,DateTime utcStart,DateTime utcEnd);
}