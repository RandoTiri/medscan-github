namespace MedScan.MAUI.Services.Api;

public static class ApiBaseAddressProvider {
    private const int ApiPort = 5183;
    private const string EnvironmentVariableName = "MEDSCAN_API_BASE_URL";
    private const string AndroidEmulatorHost = "10.0.2.2";
    private const string LocalHost = "localhost";

    public static string GetApiBaseAddress() {
        var configuredBaseUrl = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var configuredUri)) {
            return EnsureTrailingSlash(configuredUri.ToString());
        }

        if (DeviceInfo.Platform == DevicePlatform.Android) {
            var host = DeviceInfo.DeviceType == DeviceType.Virtual ? AndroidEmulatorHost : LocalHost;
            return $"http://{host}:{ApiPort}/";
        }

        return $"http://{LocalHost}:{ApiPort}/";
    }

    private static string EnsureTrailingSlash(string value) {
        return value.EndsWith('/') ? value : $"{value}/";
    }
}