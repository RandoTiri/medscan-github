using System.Net.Http.Json;
using MedScan.Shared.Models;
using MedScan.Shared.Services;

namespace MedScan.Services;

public sealed class ApiProfileService(HttpClient httpClient) : IProfileService
{
    public async Task<IReadOnlyList<ProfileSummary>> GetMyProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await httpClient.GetFromJsonAsync<List<ProfileSummary>>("api/profiles/me", cancellationToken);
        return profiles ?? [];
    }
}
