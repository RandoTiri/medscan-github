namespace MedScan.Shared.DTOs.Medication;

public sealed class TakeMedicationOnceDto
{
    public int ProfileId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }
    public bool ConfirmLastUnit { get; set; }
}
