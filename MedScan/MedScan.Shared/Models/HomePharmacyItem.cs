namespace MedScan.Shared.Models;

public sealed class HomePharmacyItem
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public Profile Profile { get; set; } = null!;
    public int MedicationId { get; set; }
    public Medication Medication { get; set; } = null!;
    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

