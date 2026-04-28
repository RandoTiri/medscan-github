using MedScan.Shared.Models.Enums;

namespace MedScan.Shared.DTOs.Medication;

public sealed class UserMedicationDto
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;

    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string? Strength { get; set; }

    public int FrequencyPerDay { get; set; }
    public List<TimeOnly> ScheduledTimes { get; set; } = [];
    public List<ScheduledDoseStatusDto> TodayDoses { get; set; } = [];
    public DateOnly? ExpiresOn { get; set; }
    public bool RemindersEnabled { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DoseStatusEnum? LatestDoseStatus { get; set; }
    public int? RemainingQuantity { get; set; }
    public string? StockWarning { get; set; }
}
