using MedScan.Shared.Models.Enums;

namespace MedScan.Shared.DTOs.Medication;

public sealed class ScheduledDoseStatusDto
{
    public TimeOnly ScheduledTime { get; set; }
    public DoseStatusEnum Status { get; set; }
}
