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

        try
        {
            await Email.Default.ComposeAsync(message);
        }
        catch
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(subject))
            {
                query.Add($"subject={Uri.EscapeDataString(subject)}");
            }

            if (!string.IsNullOrWhiteSpace(body))
            {
                query.Add($"body={Uri.EscapeDataString(body)}");
            }

            var queryString = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
            await Launcher.Default.OpenAsync(new Uri($"mailto:{emailAddress}{queryString}"));
        }
    }
}
