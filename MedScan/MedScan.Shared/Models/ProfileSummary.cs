namespace MedScan.Shared.Models;

public sealed class ProfileSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
}
