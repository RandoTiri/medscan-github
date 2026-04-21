using MedScan.Shared.Services;

namespace MedScan.Services;

public sealed class MauiExternalNavigationService : IExternalNavigationService
{
    public async Task OpenUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            await Launcher.Default.OpenAsync(uri);
        }
    }

    public async Task OpenEmailAsync(string emailAddress, string? subject = null, string? body = null)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            return;
        }

        var message = new EmailMessage
        {
            To = [emailAddress],
            Subject = subject ?? string.Empty,
            Body = body ?? string.Empty
        };

        await Email.Default.ComposeAsync(message);
    }
}
