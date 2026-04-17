using MedScan.Shared.Models;

namespace MedScan.Shared.Services;

public interface IProfileService
{
    Task<IReadOnlyList<ProfileSummary>> GetMyProfilesAsync(CancellationToken cancellationToken = default);
    Task<ProfileSummary?> GetByIdAsync(int profileId, CancellationToken cancellationToken = default);
    Task<ProfileSummary> CreatePatientProfileAsync(CreatePatientProfileRequest request, CancellationToken cancellationToken = default);
    Task<ProfileSummary?> UpdatePatientProfileAsync(int profileId, CreatePatientProfileRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeletePatientProfileAsync(int profileId, CancellationToken cancellationToken = default);
}
