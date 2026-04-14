namespace MedScan.Shared.DTOs.HomePharmacy;

public sealed class HomePharmacyItemDto
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? ActiveIngredient { get; set; }
    public string? Strength { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public DateTime AddedAt { get; set; }
}

