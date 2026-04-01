using MedScan.Api.Contracts;

namespace MedScan.Api.Services;

public interface IMedicationCatalogService
{
    Task<MedicationLookupResponse?> FindByBarcodeAsync(string barcode);
    Task<IReadOnlyList<MedicationLookupResponse>> SearchByNameAsync(string query, int limit = 20);
}
