using MedScan.Shared.Models;

namespace MedScan.Shared.Services;

public interface IMedicationCatalogClient
{
    Task<MedicationLookupResult?> FindByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicationLookupResult>> SearchByNameAsync(string query, int limit = 20, CancellationToken cancellationToken = default);
}
