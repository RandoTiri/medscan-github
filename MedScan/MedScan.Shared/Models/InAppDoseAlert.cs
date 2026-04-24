namespace MedScan.Shared.Models;

public sealed class InAppDoseAlert
{
    public int UserMedicationId { get; init; }
    public TimeOnly ScheduledTime { get; init; }
    public string MedicationName { get; init; } = string.Empty;
    public string ProfileName { get; init; } = string.Empty;
    public string? Note { get; init; }
    public DateTime TriggeredAt { get; init; }
}
