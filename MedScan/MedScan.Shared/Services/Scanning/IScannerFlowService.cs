using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;
using MedScan.Shared.Services.Catalog;

namespace MedScan.Shared.Services.Scanning;

public interface IScannerFlowService
{
    Task<BarcodeScanResult> ScanAsync(CancellationToken cancellationToken = default);
    Task OpenAppSettingsAsync();
    Task<MedicationLookupResult?> FindByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicationLookupResult>> SearchByNameAsync(string query, int limit = MedicationCatalogClient.DefaultSearchLimit, CancellationToken cancellationToken = default);
    Task<AddMedicationToScheduleResult> AddMedicationToDefaultProfileAsync(
        int medicationId,
        int frequencyPerDay,
        IReadOnlyList<TimeOnly> scheduledTimes,
        bool remindersEnabled,
        string? notes,
        DateOnly? expiresOn = null,
        CancellationToken cancellationToken = default);
}
