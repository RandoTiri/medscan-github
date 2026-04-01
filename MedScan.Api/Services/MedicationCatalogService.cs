using MedScan.Api.Contracts;
using MedScan.Api.Repositories;
using MedScan.Shared.Models;

namespace MedScan.Api.Services;

public sealed class MedicationCatalogService(IMedicationRepository medicationRepository) : IMedicationCatalogService
{
    public async Task<MedicationLookupResponse?> FindByBarcodeAsync(string barcode)
    {
        var normalized = NormalizeBarcode(barcode);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var medication = await medicationRepository.FindByBarcodeAsync(normalized);
        return medication is null ? null : MapToLookup(medication);
    }

    public async Task<IReadOnlyList<MedicationLookupResponse>> SearchByNameAsync(string query, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var cappedLimit = Math.Clamp(limit, 1, 50);
        var results = await medicationRepository.SearchByNameAsync(query.Trim());
        return results
            .Take(cappedLimit)
            .Select(MapToLookup)
            .ToList();
    }

    private static MedicationLookupResponse MapToLookup(Medication medication)
    {
        return new MedicationLookupResponse
        {
            Id = medication.Id,
            Barcode = medication.Barcode,
            Name = medication.Name,
            ActiveIngredient = medication.ActiveIngredient,
            Strength = medication.StrengthMg is int mg ? $"{mg} mg" : null,
            ShortDescription = medication.Indication,
            Warnings = medication.Warnings
        };
    }

    private static string NormalizeBarcode(string barcode)
    {
        return new string(barcode.Trim().Where(char.IsDigit).ToArray());
    }
}
