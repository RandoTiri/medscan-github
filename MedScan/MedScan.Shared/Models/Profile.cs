using MedScan.Shared.Models.Enums;

namespace MedScan.Shared.Models; 
public class Profile {
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public ProfileTypeEnum ProfileType { get; set; }
    public DateOnly? BirthDate { get; set; }
    public ICollection<UserMedication> Medications { get; set; } = new List<UserMedication>();
}