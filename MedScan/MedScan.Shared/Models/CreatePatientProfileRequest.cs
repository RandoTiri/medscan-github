namespace MedScan.Shared.Models;

public sealed class CreatePatientProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
}
