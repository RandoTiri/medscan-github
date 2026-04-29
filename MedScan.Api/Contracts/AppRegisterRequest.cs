namespace MedScan.Api.Contracts;

public class AppRegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
}
