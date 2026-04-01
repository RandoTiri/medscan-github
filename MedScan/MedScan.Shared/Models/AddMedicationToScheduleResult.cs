namespace MedScan.Shared.Models;

public sealed class AddMedicationToScheduleResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int? UserMedicationId { get; init; }
}
