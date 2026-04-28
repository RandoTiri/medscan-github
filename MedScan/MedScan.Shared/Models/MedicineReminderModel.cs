using MedScan.Shared.Models.Enums;

namespace MedScan.Shared.Models;

public sealed class MedicineReminderModel
{
    public int UserMedicationId { get; init; }
    public string MedicationName { get; init; } = string.Empty;
    public string ProfileName { get; init; } = string.Empty;
    public string Dosage { get; init; } = string.Empty;
    public bool RemindersEnabled { get; init; }
    public int Frequency { get; init; }
    public MedicationScheduleUnit ScheduleUnit { get; init; }
    public DateOnly StartDate { get; init; }
    public IReadOnlyList<TimeOnly> ReminderTimes { get; init; } = [];
    public IReadOnlyList<int> WeeklyDays { get; init; } = [];
}
