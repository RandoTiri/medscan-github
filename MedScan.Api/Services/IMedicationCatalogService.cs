using MedScan.Shared.Models;

namespace MedScan.Api.Services;

public interface IMedicationCatalogService
{
    Task<MedicationLookupResult?> FindByBarcodeAsync(string barcode);
    Task<IReadOnlyList<MedicationLookupResult>> SearchByNameAsync(string query, int limit = 20);
}
