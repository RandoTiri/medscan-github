using System.Net;
using System.Net.Http.Json;
using MedScan.Shared.DTOs.Medication;

namespace MedScan.Shared.Services.Catalog;

public sealed class MedicationCatalogClient(HttpClient httpClient) : IMedicationCatalogClient
{
    public const int DefaultSearchLimit = 20;
    private const int MinSearchLimit = 1;
    private const int MaxSearchLimit = 50;

    public async Task<MedicationLookupResult?> FindByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        using var response = await httpClient.GetAsync(
            $"api/medication-catalog/by-barcode/{Uri.EscapeDataString(barcode.Trim())}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MedicationLookupResult>(cancellationToken);
    }

    public async Task<IReadOnlyList<MedicationLookupResult>> SearchByNameAsync(string query, int limit = DefaultSearchLimit, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var url = $"api/medication-catalog/search?query={Uri.EscapeDataString(query.Trim())}&limit={Math.Clamp(limit, MinSearchLimit, MaxSearchLimit)}";
        var results = await httpClient.GetFromJsonAsync<List<MedicationLookupResult>>(url, cancellationToken);
        return results ?? [];
    }
}
