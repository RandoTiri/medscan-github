using MedScan.Api.Repositories;
using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;

namespace MedScan.Api.Services.Medications;

public sealed class MedicationStockService(IHomePharmacyRepository homePharmacyRepository) {
    public async Task<MedicationStockResult> DecrementForCompletedDoseAsync(
        UserMedication userMedication,
        DoseStatusEnum newStatus,
        DoseStatusEnum? previousStatus) {
        var shouldDecrease = newStatus == DoseStatusEnum.Done && previousStatus != DoseStatusEnum.Done;
        if (!shouldDecrease) {
            return MedicationStockResult.NotApplicable;
        }

        var stockItem = await homePharmacyRepository
            .FindNewestTrackedByProfileAndMedicationAsync(userMedication.ProfileId,userMedication.MedicationId);
        if (stockItem is null || stockItem.Quantity <= 0) {
            return MedicationStockResult.NotApplicable;
        }

        if (stockItem.Quantity == 1) {
            homePharmacyRepository.Remove(stockItem);
            return new MedicationStockResult(
                RemainingQuantity: 0,
                RemovedFromEverywhere: true,
                Warning: MedicationConstants.Messages.LastPackWarning);
        }

        stockItem.Quantity -= 1;
        var remaining = stockItem.Quantity;
        var warning = remaining <= MedicationConstants.LowStockWarningThreshold
            ? MedicationConstants.Messages.LowStockRemaining(remaining)
            : null;

        return new MedicationStockResult(remaining,RemovedFromEverywhere: false,Warning: warning);
    }

    public TakeMedicationOnceResultDto? ValidateForTakeOnce(HomePharmacyItem? stockItem,TakeMedicationOnceDto dto) {
        if (stockItem is null || stockItem.Quantity <= 0) {
            return Failure(MedicationConstants.Messages.StockEmpty);
        }

        if (stockItem.Quantity == 1 && !dto.ConfirmLastUnit) {
            return new TakeMedicationOnceResultDto {
                Success = false,
                RequiresLastUnitConfirmation = true,
                RemainingQuantity = 1,
                Message = MedicationConstants.Messages.LastUnitConfirmationNeeded
            };
        }

        var quantityToTake = NormalizeQuantity(dto.Quantity);
        if (stockItem.Quantity < quantityToTake) {
            return Failure(MedicationConstants.Messages.StockBelowRequested(stockItem.Quantity));
        }

        return null;
    }

    public (int remaining,bool removed) Consume(HomePharmacyItem stockItem,int quantityToTake) {
        var remaining = stockItem.Quantity - quantityToTake;
        if (remaining <= 0) {
            homePharmacyRepository.Remove(stockItem);
            return (0,true);
        }

        stockItem.Quantity = remaining;
        return (remaining,false);
    }

    public static int NormalizeQuantity(int quantity) => quantity <= 0 ? 1 : quantity;

    private static TakeMedicationOnceResultDto Failure(string message) =>
        new() {
            Success = false,
            Message = message
        };
}

public readonly record struct MedicationStockResult(
    int? RemainingQuantity,
    bool RemovedFromEverywhere,
    string? Warning) {
    public static MedicationStockResult NotApplicable => new(null,false,null);
}
