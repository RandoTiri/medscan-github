using MedScan.Shared.Models.Enums;

namespace MedScan.Shared.Models;

public sealed class MedicationLookupResult
{
    public int Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ActiveIngredient { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public string? MedicationForm { get; set; }
    public string? PackSize { get; set; }
    public string? ShortDescription { get; set; }
    public string? Warnings { get; set; }
}
