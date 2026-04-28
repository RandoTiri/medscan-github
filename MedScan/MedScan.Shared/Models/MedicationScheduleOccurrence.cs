namespace MedScan.Shared.Models;

public sealed record MedicationScheduleOccurrence(
    int SlotIndex,
    DateOnly Date,
    TimeOnly Time);
