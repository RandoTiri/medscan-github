namespace MedScan.Shared.Models; 
public class DoseLog {
    public int Id { get; set; }
    public int UserMedicationId { get; set; }
    public UserMedication UserMedication { get; set; } = null!;
    public DateTime ScheduledTime { get; set; }
    public DateTime? TakenAt { get; set; }
    public DoseStatus Status { get; set; } = DoseStatus.Pending;
    public string? ConfirmedByUserId { get; set; }
}

public enum DoseStatus { Pending, Done, Missed, Skipped }