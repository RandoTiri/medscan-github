namespace MedScan.Shared.Services;

public interface IExternalNavigationService
{
    Task OpenUrlAsync(string url);
    Task OpenEmailAsync(string emailAddress, string? subject = null, string? body = null);
}
