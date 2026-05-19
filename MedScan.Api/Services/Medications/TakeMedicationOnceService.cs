using MedScan.Api.Repositories;
using MedScan.Api.Repositories.Medications;
using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;

namespace MedScan.Api.Services.Medications;

public sealed class TakeMedicationOnceService(
    IMedicationRepository medicationRepository,
    IUserMedicationRepository userMedicationRepository,
    IHomePharmacyRepository homePharmacyRepository,
    DoseLogService doseLogService,
    MedicationStockService stockService) {
    public async Task<TakeMedicationOnceResultDto> TakeOnceAsync(int medicationId,TakeMedicationOnceDto dto) {
        if (dto.ProfileId <= 0) {
            return Failure(MedicationConstants.Messages.ProfileMissing);
        }

        var medication = await medicationRepository.FindByIdAsync(medicationId);
        if (medication is null) {
            return Failure(MedicationConstants.Messages.MedicationNotFound);
        }

        var stockItem = await homePharmacyRepository
            .FindNewestTrackedByProfileAndMedicationAsync(dto.ProfileId,medicationId);
        var stockCheck = stockService.ValidateForTakeOnce(stockItem,dto);
        if (stockCheck is not null) {
            return stockCheck;
        }

        var medicationForLog = await EnsureUserMedicationForLogAsync(dto.ProfileId,medicationId,dto.Notes);
        doseLogService.AddTakenNow(medicationForLog.Id);

        var quantityToTake = MedicationStockService.NormalizeQuantity(dto.Quantity);
        var (remaining,removed) = stockService.Consume(stockItem!,quantityToTake);

        await userMedicationRepository.SaveChangesAsync();

        return new TakeMedicationOnceResultDto {
            Success = true,
            RemainingQuantity = removed ? 0 : remaining,
            RemovedFromHomePharmacy = removed,
            Message = MedicationConstants.Messages.TakeOnceSaved
        };
    }

    private async Task<UserMedication> EnsureUserMedicationForLogAsync(int profileId,int medicationId,string? notes) {
        var existing = await userMedicationRepository
            .GetNewestActiveTrackedByProfileAndMedicationAsync(profileId,medicationId);
        if (existing is not null) {
            return existing;
        }

        var fallback = new UserMedication {
            ProfileId = profileId,
            MedicationId = medicationId,
            Frequency = 1,
            ScheduleUnit = MedicationScheduleUnit.Day,
            ScheduledTimesJson = MedicationConstants.DefaultScheduledTimeJson,
            WeeklyDaysJson = MedicationConstants.EmptyJsonArray,
            StartDate = DateOnly.FromDateTime(DateTime.Now),
            RemindersEnabled = false,
            Notes = string.IsNullOrWhiteSpace(notes) ? MedicationConstants.Messages.TakeOnceFallbackNote : notes.Trim(),
            IsActive = false,
            AddedAt = DateTime.UtcNow
        };

        await userMedicationRepository.AddAsync(fallback);
        await userMedicationRepository.SaveChangesAsync();
        return fallback;
    }

    private static TakeMedicationOnceResultDto Failure(string message) =>
        new() {
            Success = false,
            Message = message
        };
}