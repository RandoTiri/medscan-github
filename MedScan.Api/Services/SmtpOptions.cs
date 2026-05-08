namespace MedScan.Api.Services;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = "MedScan";
    public string FromEmail { get; set; } = string.Empty;
    public bool UseStartTls { get; set; } = true;
}
