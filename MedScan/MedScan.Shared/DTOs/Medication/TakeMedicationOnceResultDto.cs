namespace MedScan.Shared.DTOs.Medication;

public sealed class TakeMedicationOnceResultDto
{
    public bool Success { get; set; }
    public bool RequiresLastUnitConfirmation { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? RemainingQuantity { get; set; }
    public bool RemovedFromHomePharmacy { get; set; }
}
