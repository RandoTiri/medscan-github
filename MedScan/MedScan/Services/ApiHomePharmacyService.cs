using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MedScan.Shared.DTOs.HomePharmacy;
using MedScan.Shared.Services;

namespace MedScan.Services;

public sealed class ApiHomePharmacyService(HttpClient httpClient) : IHomePharmacyService
{
    public async Task<IReadOnlyList<HomePharmacyItemDto>> GetByProfileIdAsync(int profileId, CancellationToken cancellationToken = default)
    {
        var items = await httpClient.GetFromJsonAsync<List<HomePharmacyItemDto>>(
            $"api/home-pharmacy?profileId={profileId}",
            cancellationToken);

        return items ?? [];
    }

    public Task<HomePharmacyItemDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return httpClient.GetFromJsonAsync<HomePharmacyItemDto>($"api/home-pharmacy/{id}", cancellationToken);
    }

    public async Task<HomePharmacyItemDto> AddAsync(AddHomePharmacyItemDto dto, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/home-pharmacy", dto, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadErrorMessageAsync(response, cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<HomePharmacyItemDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Home pharmacy add response was empty.");
    }

    public async Task<HomePharmacyItemDto?> UpdateAsync(int id, UpdateHomePharmacyItemDto dto, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/home-pharmacy/{id}", dto, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadErrorMessageAsync(response, cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<HomePharmacyItemDto>(cancellationToken: cancellationToken);
    }

    public async Task<bool> RemoveAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/home-pharmacy/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "Toiming ebaõnnestus.";
        }

        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("message", out var messageElement) &&
                messageElement.ValueKind == JsonValueKind.String)
            {
                return messageElement.GetString() ?? "Toiming ebaõnnestus.";
            }
        }
        catch
        {
        }

        return raw;
    }
}
