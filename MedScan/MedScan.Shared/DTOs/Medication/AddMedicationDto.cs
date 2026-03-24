namespace MedScan.Shared.DTOs.Medication; 
public class AddMedicationDto {
    public int MedicationId { get; set; }
    public int ProfileId { get; set; }
    public int Frequency { get; set; }
    List<string>? ScheduledTimes { get; set; }
    public bool RemindersEnabled { get; set; }
}