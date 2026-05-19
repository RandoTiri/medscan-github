using MedScan.Shared.Models;

namespace MedScan.Api.Repositories.Profiles;

public interface IProfileRepository {
    Task<bool> ExistsForUserAsync(int profileId,string userId,CancellationToken cancellationToken = default);
    Task<Profile?> GetForUserAsync(int profileId,string userId,CancellationToken cancellationToken = default);
    Task<Profile?> GetTrackedForUserAsync(int profileId,string userId,CancellationToken cancellationToken = default);
    Task<List<Profile>> GetAllForUserAsync(string userId,CancellationToken cancellationToken = default);
    Task<List<Profile>> GetTrackedAllForUserAsync(string userId,CancellationToken cancellationToken = default);
    Task<int?> GetDefaultProfileIdForUserAsync(string userId,CancellationToken cancellationToken = default);
    Task AddAsync(Profile profile,CancellationToken cancellationToken = default);
    void Remove(Profile profile);
    void RemoveRange(IEnumerable<Profile> profiles);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}