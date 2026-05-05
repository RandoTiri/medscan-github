using MedScan.Api.Repositories;
using MedScan.Api.Data;
using MedScan.Shared.DTOs.HomePharmacy;
using MedScan.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Services;

public sealed class HomePharmacyService(
    IHomePharmacyRepository homePharmacyRepository,
    IMedicationRepository medicationRepository,
    AppDbContext dbContext) : IHomePharmacyService
{
    public async Task<IEnumerable<HomePharmacyItemDto>> GetByProfileIdAsync(int profileId)
    {
        var items = await homePharmacyRepository.GetByProfileIdAsync(profileId);
        return items.Select(MapToDto);
    }

    public async Task<HomePharmacyItemDto?> GetByIdAsync(int id)
    {
        var item = await homePharmacyRepository.GetByIdAsync(id);
        return item is null ? null : MapToDto(item);
    }

    public async Task<HomePharmacyItemDto> AddAsync(AddHomePharmacyItemDto dto)
    {
        var medication = await medicationRepository.FindByIdAsync(dto.MedicationId);
        if (medication is null)
        {
            throw new ArgumentException($"Medication with id {dto.MedicationId} was not found.");
        }

        var item = new HomePharmacyItem
        {
            ProfileId = dto.ProfileId,
            MedicationId = dto.MedicationId,
            PackageNumber = dto.PackageNumber,
            BatchNumber = string.IsNullOrWhiteSpace(dto.BatchNumber) ? null : dto.BatchNumber.Trim(),
            ExpiresOn = dto.ExpiresOn,
            Quantity = Math.Max(1, dto.Quantity),
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            AddedAt = DateTime.UtcNow
        };

        await homePharmacyRepository.AddAsync(item);
        await homePharmacyRepository.SaveChangesAsync();

        var created = await homePharmacyRepository.GetByIdAsync(item.Id)
            ?? throw new InvalidOperationException("Created home pharmacy item could not be reloaded.");

        return MapToDto(created);
    }

    public async Task<HomePharmacyItemDto?> UpdateAsync(int id, UpdateHomePharmacyItemDto dto)
    {
        var existing = await homePharmacyRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return null;
        }

        existing.Quantity = Math.Max(1, dto.Quantity);
        existing.PackageNumber = dto.PackageNumber;
        existing.BatchNumber = string.IsNullOrWhiteSpace(dto.BatchNumber) ? null : dto.BatchNumber.Trim();
        existing.ExpiresOn = dto.ExpiresOn;
        existing.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

        await homePharmacyRepository.SaveChangesAsync();

        var updated = await homePharmacyRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Updated home pharmacy item could not be reloaded.");

        return MapToDto(updated);
    }

    public async Task<bool> RemoveAsync(int id)
    {
        var existing = await homePharmacyRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return false;
        }

        var scheduleItems = await dbContext.UserMedications
            .Where(x => x.ProfileId == existing.ProfileId && x.MedicationId == existing.MedicationId)
            .ToListAsync();

        if (scheduleItems.Count > 0)
        {
            foreach (var scheduleItem in scheduleItems)
            {
                scheduleItem.IsActive = false;
            }
        }

        homePharmacyRepository.Remove(existing);
        await homePharmacyRepository.SaveChangesAsync();
        return true;
    }

    private static HomePharmacyItemDto MapToDto(HomePharmacyItem item)
    {
        return new HomePharmacyItemDto
        {
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
}
