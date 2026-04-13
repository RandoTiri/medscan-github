using MedScan.Shared.Models;

namespace MedScan.Shared.Services;

public interface IProfileService
{
    Task<IReadOnlyList<ProfileSummary>> GetMyProfilesAsync(CancellationToken cancellationToken = default);
}
