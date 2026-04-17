using System.Net.Http.Json;
using System.Net;
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

    public async Task<ProfileSummary?> GetByIdAsync(int profileId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/profiles/{profileId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProfileSummary>(cancellationToken);
    }

    public async Task<ProfileSummary> CreatePatientProfileAsync(CreatePatientProfileRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/profiles/patient", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var createdProfile = await response.Content.ReadFromJsonAsync<ProfileSummary>(cancellationToken);
        return createdProfile ?? new ProfileSummary
        {
            Name = request.Name,
            Gender = request.Gender,
            Type = "Patsient",
            BirthDate = request.BirthDate
        };
    }

    public async Task<ProfileSummary?> UpdatePatientProfileAsync(int profileId, CreatePatientProfileRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"api/profiles/{profileId}", request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProfileSummary>(cancellationToken);
    }

    public async Task<bool> DeletePatientProfileAsync(int profileId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"api/profiles/{profileId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }
}
