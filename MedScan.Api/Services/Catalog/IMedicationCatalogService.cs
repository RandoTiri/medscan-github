using MedScan.Shared.DTOs.Medication;

namespace MedScan.Api.Services.Catalog;

public interface IMedicationCatalogService
{
    Task<MedicationLookupResult?> FindByBarcodeAsync(string barcode);
    Task<IReadOnlyList<MedicationLookupResult>> SearchByNameAsync(string query, int limit = 20);
}
