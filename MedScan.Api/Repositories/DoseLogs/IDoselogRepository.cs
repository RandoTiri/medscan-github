using MedScan.Shared.Models;

namespace MedScan.Api.Repositories;

public interface IDoseLogRepository {
    void Add(DoseLog log);
    Task<List<DoseLog>> GetByProfileInRangeAsync(int profileId,DateTime utcStart,DateTime utcEnd);
}