using Microsoft.Maui.Devices;

namespace MedScan.Services;

public static class ApiBaseAddressProvider {
    private const int ApiPort = 5183;

    public static string GetApiBaseAddress() {
        var configuredBaseUrl = Environment.GetEnvironmentVariable("MEDSCAN_API_BASE_URL");
        if (Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var configuredUri)) {
            return EnsureTrailingSlash(configuredUri.ToString());
        }

        if (DeviceInfo.Platform == DevicePlatform.Android) {
            var host = DeviceInfo.DeviceType == DeviceType.Virtual ? "10.0.2.2" : "localhost";
            return $"http://{host}:{ApiPort}/";
        }

        return $"http://localhost:{ApiPort}/";
    }

    private static string EnsureTrailingSlash(string value) {
        return value.EndsWith("/") ? value : $"{value}/";
    }
}
