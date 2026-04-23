using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;

namespace MedScan.Shared.Services;

public sealed class ScannerFlowService(
    IAuthService authService,
    IBarcodeScannerService barcodeScannerService,
    IMedicationCatalogClient medicationCatalogClient,
    IMedicationService medicationService) : IScannerFlowService
{
    private readonly SemaphoreSlim _scanGate = new(1, 1);

    public async Task<BarcodeScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        await _scanGate.WaitAsync(cancellationToken);

        try
        {
            return await barcodeScannerService.ScanAsync(cancellationToken);
        }
        finally
        {
            _scanGate.Release();
        }
    }

    public Task<MedicationLookupResult?> FindByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return medicationCatalogClient.FindByBarcodeAsync(barcode, cancellationToken);
    }

    public Task OpenAppSettingsAsync()
    {
        return barcodeScannerService.OpenAppSettingsAsync();
    }

    public Task<IReadOnlyList<MedicationLookupResult>> SearchByNameAsync(string query, int limit = 20, CancellationToken cancellationToken = default)
    {
        return medicationCatalogClient.SearchByNameAsync(query, limit, cancellationToken);
    }

    public async Task<AddMedicationToScheduleResult> AddMedicationToDefaultProfileAsync(
        int medicationId,
        int frequencyPerDay,
        IReadOnlyList<TimeOnly> scheduledTimes,
        bool remindersEnabled,
        string? notes,
        DateOnly? expiresOn = null,
        CancellationToken cancellationToken = default)
    {
        if (medicationId <= 0)
        {
            return new AddMedicationToScheduleResult { Message = "Ravimi ID on vigane." };
        }

        if (frequencyPerDay <= 0)
        {
            return new AddMedicationToScheduleResult { Message = "Sagedus peab olema vahemikus 1-24." };
        }

        if (scheduledTimes.Count == 0)
        {
            return new AddMedicationToScheduleResult { Message = "Lisa vahemalt uks manustamise aeg." };
        }

        if (scheduledTimes.Count != frequencyPerDay)
        {
            return new AddMedicationToScheduleResult { Message = "Meeldetuletuse aegade arv peab vastama paevasele sagedusele." };
        }

        await authService.InitializeAsync();

        if (!authService.IsLoggedIn)
        {
            return new AddMedicationToScheduleResult { Message = "Palun logi sisse." };
        }

        var profileId = authService.CurrentUser?.DefaultProfileId;
        if (!profileId.HasValue)
        {
            return new AddMedicationToScheduleResult { Message = "Vaikimisi profiili ei leitud." };
        }

        var normalizedTimes = scheduledTimes
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        var dto = new AddMedicationDto
        {
            ProfileId = profileId.Value,
            MedicationId = medicationId,
            FrequencyPerDay = Math.Clamp(frequencyPerDay, 1, 24),
            ScheduledTimes = normalizedTimes,
            ExpiresOn = expiresOn,
            RemindersEnabled = remindersEnabled,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };

        try
        {
            var saved = await medicationService.AddToScheduleAsync(dto);

            return new AddMedicationToScheduleResult
            {
                Success = true,
                Message = "Ravim lisati raviplaani.",
                UserMedicationId = saved.Id
            };
        }
        catch
        {
            return new AddMedicationToScheduleResult
            {
                Message = "Ravimi lisamine ebaonnestus. Proovi uuesti."
            };
        }
    }
}
