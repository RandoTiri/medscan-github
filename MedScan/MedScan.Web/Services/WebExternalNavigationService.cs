using MedScan.Shared.Services;
using Microsoft.JSInterop;

namespace MedScan.Web.Services;

public sealed class WebExternalNavigationService(IJSRuntime jsRuntime) : IExternalNavigationService
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public Task OpenUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Task.CompletedTask;
        }

        return _jsRuntime.InvokeVoidAsync("open", url, "_blank").AsTask();
    }

    public Task OpenEmailAsync(string emailAddress, string? subject = null, string? body = null)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            return Task.CompletedTask;
        }

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
        var mailto = $"mailto:{emailAddress}{queryString}";

        return _jsRuntime.InvokeVoidAsync("open", mailto, "_self").AsTask();
    }
}
