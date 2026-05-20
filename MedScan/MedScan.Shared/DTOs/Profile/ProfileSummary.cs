namespace MedScan.Shared.DTOs.Profile;

public sealed class ProfileSummary {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
}