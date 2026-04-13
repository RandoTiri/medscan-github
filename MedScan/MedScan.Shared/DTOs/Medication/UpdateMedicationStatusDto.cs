using MedScan.Shared.Models.Enums;

namespace MedScan.Shared.DTOs.Medication;

public sealed class UpdateMedicationStatusDto
{
    public TimeOnly? ScheduledTime { get; set; }
    public DoseStatusEnum Status { get; set; }
}
