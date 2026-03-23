namespace MedScan.Shared.Models;
public sealed class MedicineReminderModel {
    public int UserMedicationId { get; init; }
    public string MedicationName { get; init; } = string.Empty;
    public string ProfileName { get; init; } = string.Empty;
    public string Dosage { get; init; } = string.Empty;
    public bool RemindersEnabled { get; init; }
    public IReadOnlyList<TimeOnly> ReminderTimes { get; init; } = [];
}
