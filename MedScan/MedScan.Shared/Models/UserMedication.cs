namespace MedScan.Shared.Models; 
public class UserMedication {
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public Profile Profile { get; set; } = null!;
    public int MedicationId { get; set; }
    public Medication Medication { get; set; } = null!;
    public int FrequencyPerDay { get; set; }
    public string ScheduledTimesJson { get; set; } = "[]";
    public bool RemindersEnabled { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public ICollection<DoseLog> DoseLogs { get; set; } = new List<DoseLog>();
}
