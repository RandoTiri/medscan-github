namespace MedScan.Shared.Models; 
public class Profile {
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public ProfileType Type { get; set; }  // Self, Child, Elderly, Patient
    public DateOnly? BirthDate { get; set; }
    public ICollection<UserMedication> Medications { get; set; } = new List<UserMedication>();
}

public enum ProfileType { Self, Child, Elderly, Patient }
