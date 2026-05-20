using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MedScan.Shared.DTOs.HomePharmacy;
using MedScan.Shared.Services.HomePharmacy;
using Microsoft.Extensions.Logging;

namespace MedScan.MAUI.Services.Api;

public sealed class ApiHomePharmacyService(
    HttpClient httpClient,
    ILogger<ApiHomePharmacyService> logger) : IHomePharmacyService {
    private readonly ILogger<ApiHomePharmacyService> _logger = logger;

    public async Task<IReadOnlyList<HomePharmacyItemDto>> GetByProfileIdAsync(int profileId, CancellationToken cancellationToken = default) {
        var items = await httpClient.GetFromJsonAsync<List<HomePharmacyItemDto>>(
            $"api/home-pharmacy?profileId={profileId}",
            cancellationToken);

        return items ?? [];
    }

    public async Task<HomePharmacyItemDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) {
        var response = await httpClient.GetAsync($"api/home-pharmacy/{id}",cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HomePharmacyItemDto>(cancellationToken);
    }

    public async Task<HomePharmacyItemDto> AddAsync(AddHomePharmacyItemDto dto, CancellationToken cancellationToken = default) {
        var response = await httpClient.PostAsJsonAsync("api/home-pharmacy", dto, cancellationToken);
        if (!response.IsSuccessStatusCode) 
            throw new InvalidOperationException(await ReadErrorMessageAsync(response, cancellationToken));

        return await response.Content.ReadFromJsonAsync<HomePharmacyItemDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Home pharmacy add response was empty.");
    }

    public async Task<HomePharmacyItemDto?> UpdateAsync(int id, UpdateHomePharmacyItemDto dto, CancellationToken cancellationToken = default) {
        var response = await httpClient.PutAsJsonAsync($"api/home-pharmacy/{id}", dto, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        if (!response.IsSuccessStatusCode) 
            throw new InvalidOperationException(await ReadErrorMessageAsync(response, cancellationToken));

        return await response.Content.ReadFromJsonAsync<HomePharmacyItemDto>(cancellationToken: cancellationToken);
    }

    public async Task<bool> RemoveAsync(int id, CancellationToken cancellationToken = default) {
        var response = await httpClient.DeleteAsync($"api/home-pharmacy/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return false;

        response.EnsureSuccessStatusCode();
        return true;
    }

    private async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken) {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(raw)) return "Toiming ebaõnnestus.";

        try
        {
            var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("message", out var messageElement) &&
                messageElement.ValueKind == JsonValueKind.String)
            {
                return messageElement.GetString() ?? "Toiming ebaõnnestus.";
            }
        }
        catch(JsonException ex)
        {
            _logger.LogWarning(ex,"Failed to parse home pharmacy API error response.");
        }
        return raw;
    }
}
