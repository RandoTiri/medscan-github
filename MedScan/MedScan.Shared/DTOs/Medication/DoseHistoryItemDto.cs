using MedScan.Shared.Models.Enums;

namespace MedScan.Shared.DTOs.Medication;

public sealed class DoseHistoryItemDto
{
    public string MedicationName { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public DateTime ScheduledTime { get; set; }
    public DoseStatusEnum Status { get; set; }
}
