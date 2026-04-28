using MedScan.Shared.Models.Enums;

namespace MedScan.Shared.DTOs.Medication;

public sealed class AddMedicationDto
{
    public int ProfileId { get; set; }
    public int MedicationId { get; set; }
    public int FrequencyPerDay { get; set; }
    public MedicationScheduleUnit ScheduleUnit { get; set; } = MedicationScheduleUnit.Day;
    public List<TimeOnly> ScheduledTimes { get; set; } = [];
    public List<int> WeeklyDays { get; set; } = [];
    public DateOnly? StartDate { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public bool RemindersEnabled { get; set; } = true;
    public string? Notes { get; set; }
}
