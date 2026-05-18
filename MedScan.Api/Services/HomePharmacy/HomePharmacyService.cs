using MedScan.Api.Repositories;
using MedScan.Api.Repositories.Medications;
using MedScan.Shared.DTOs.HomePharmacy;
using MedScan.Shared.Models;
using MedScan.Shared.Services;

namespace MedScan.Api.Services;

public sealed class HomePharmacyService(
    IHomePharmacyRepository homePharmacyRepository,
    IMedicationRepository medicationRepository,
    IUserMedicationRepository userMedicationRepository) : IHomePharmacyService {
    private static readonly IReadOnlyDictionary<string,DateOnly> SeededExpiryByBarcode =
        new Dictionary<string,DateOnly>(StringComparer.Ordinal) {
            ["3800010640916"] = new(2030,7,31),
            ["3800010646529"] = new(2030,3,31),
            ["5055565732748"] = new(2030,3,31),
            ["5010123729189"] = new(2027,6,30),
            ["4013054029832"] = new(2028,11,30),
            ["04030855234233"] = new(2030,2,28),
            ["05290931027022"] = new(2029,10,31),
            ["07613421029043"] = new(2027,6,30),
            ["03582910055372"] = new(2027,12,31),
            ["4742041002907"] = new(2026,7,31),
            ["5000158104273"] = new(2027,1,31),
            ["09008732010848"] = new(2025,9,30),
            ["7612711550243"] = new(2027,5,31),
            ["08436029300173"] = new(2026,6,30),
            ["05400835010956"] = new(2026,6,30),
            ["4013054018279"] = new(2027,11,30),
        };

    public async Task<IReadOnlyList<HomePharmacyItemDto>> GetByProfileIdAsync(int profileId,CancellationToken cancellationToken = default) {
        var items = await homePharmacyRepository.GetByProfileIdAsync(profileId);
        return items.Select(MapToDto).ToList();
    }

    public async Task<HomePharmacyItemDto?> GetByIdAsync(int id,CancellationToken cancellationToken = default) {
        var item = await homePharmacyRepository.GetByIdAsync(id);
        return item is null ? null : MapToDto(item);
    }

    public async Task<HomePharmacyItemDto> AddAsync(AddHomePharmacyItemDto dto,CancellationToken cancellationToken = default) {
        var medication = await medicationRepository.FindByIdAsync(dto.MedicationId);
        if (medication is null) {
            throw new ArgumentException($"Medication with id {dto.MedicationId} was not found.");
        }

        var item = new HomePharmacyItem {
            ProfileId = dto.ProfileId,
            MedicationId = dto.MedicationId,
            PackageNumber = dto.PackageNumber,
            BatchNumber = string.IsNullOrWhiteSpace(dto.BatchNumber) ? null : dto.BatchNumber.Trim(),
            ExpiresOn = dto.ExpiresOn,
            Quantity = ResolveInitialQuantity(dto.Quantity,medication.PackSize),
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            AddedAt = DateTime.UtcNow
        };

        if (item.ExpiresOn is null) {
            item.ExpiresOn = await ResolveFallbackExpiryAsync(medication,dto.MedicationId,cancellationToken);
        }

        await homePharmacyRepository.AddAsync(item);
        await homePharmacyRepository.SaveChangesAsync();

        var created = await homePharmacyRepository.GetByIdAsync(item.Id)
            ?? throw new InvalidOperationException("Created home pharmacy item could not be reloaded.");

        return MapToDto(created);
    }

    public async Task<HomePharmacyItemDto?> UpdateAsync(int id,UpdateHomePharmacyItemDto dto,CancellationToken cancellationToken = default) {
        var existing = await homePharmacyRepository.GetByIdAsync(id);
        if (existing is null) {
            return null;
        }

        existing.Quantity = Math.Max(1,dto.Quantity);
        existing.PackageNumber = dto.PackageNumber;
        existing.BatchNumber = string.IsNullOrWhiteSpace(dto.BatchNumber) ? null : dto.BatchNumber.Trim();
        existing.ExpiresOn = dto.ExpiresOn;
        existing.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

        await homePharmacyRepository.SaveChangesAsync();

        var updated = await homePharmacyRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Updated home pharmacy item could not be reloaded.");

        return MapToDto(updated);
    }

    public async Task<bool> RemoveAsync(int id,CancellationToken cancellationToken = default) {
        var existing = await homePharmacyRepository.GetByIdAsync(id);
        if (existing is null) {
            return false;
        }

        var activeSchedules = await userMedicationRepository
            .GetTrackedActiveByProfileAndMedicationAsync(existing.ProfileId,existing.MedicationId);

        foreach (var schedule in activeSchedules) {
            schedule.IsActive = false;
        }

        homePharmacyRepository.Remove(existing);
        await homePharmacyRepository.SaveChangesAsync();
        return true;
    }

    private async Task<DateOnly?> ResolveFallbackExpiryAsync(Medication medication,int medicationId,CancellationToken cancellationToken) {
        if (!string.IsNullOrWhiteSpace(medication.Barcode) &&
            SeededExpiryByBarcode.TryGetValue(medication.Barcode,out var seededExpiry)) {
            return seededExpiry;
        }

        return await homePharmacyRepository.FindOldestExpiryForMedicationAsync(medicationId,cancellationToken);
    }

    private static HomePharmacyItemDto MapToDto(HomePharmacyItem item) {
        return new HomePharmacyItemDto {
            Id = item.Id,
            ProfileId = item.ProfileId,
            ProfileName = item.Profile?.Name ?? string.Empty,
            MedicationId = item.MedicationId,
            MedicationName = item.Medication?.Name ?? string.Empty,
            Barcode = item.Medication?.Barcode,
            ActiveIngredient = item.Medication?.ActiveIngredient,
            Strength = item.Medication?.StrengthMg,
            PackSize = item.Medication?.PackSize,
            MedicationForm = item.Medication?.MedicationForm,
            PackageNumber = item.PackageNumber,
            BatchNumber = item.BatchNumber,
            ExpiresOn = item.ExpiresOn,
            Quantity = item.Quantity,
            Notes = item.Notes,
            Indication = item.Medication?.Indication,
            Warnings = item.Medication?.Warnings,
            PdfUrl = item.Medication?.PdfUrl,
            Manufacturer = item.Medication?.Manufacturer,
            MarketingAuthNumber = item.Medication?.MarketingAuthNr,
            AddedAt = item.AddedAt
        };
    }

    private static int ResolveInitialQuantity(int requestedQuantity,string? packSize) {
        if (requestedQuantity > 1) {
            return requestedQuantity;
        }

        var parsedPackQuantity = ParsePackSizeQuantity(packSize);
        return parsedPackQuantity > 0 ? parsedPackQuantity : 1;
    }

    private static int ParsePackSizeQuantity(string? packSize) {
        if (string.IsNullOrWhiteSpace(packSize)) {
            return 0;
        }

        var digits = new string(packSize.Where(char.IsDigit).ToArray());
        return int.TryParse(digits,out var parsed) && parsed > 0 ? parsed : 0;
    }
}