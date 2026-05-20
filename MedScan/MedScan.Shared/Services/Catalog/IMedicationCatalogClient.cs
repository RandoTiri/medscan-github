using MedScan.Shared.DTOs.Medication;

namespace MedScan.Shared.Services.Catalog;

public interface IMedicationCatalogClient
{
    Task<MedicationLookupResult?> FindByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicationLookupResult>> SearchByNameAsync(string query, int limit = MedicationCatalogClient.DefaultSearchLimit, CancellationToken cancellationToken = default);
}
