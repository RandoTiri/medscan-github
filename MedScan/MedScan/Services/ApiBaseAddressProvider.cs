using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MedScan.Services;

public static class ApiBaseAddressProvider {
    private const int ApiPort = 5183;

    public static string GetApiBaseAddress() {
        var wifiIp = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni =>
                (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                 ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                ni.OperationalStatus == OperationalStatus.Up)
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
            .Select(u => u.Address)
            .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

        if (wifiIp is null) {
            var fallbackIp = DeviceInfo.Platform == DevicePlatform.Android ? "10.0.2.2" : "localhost";
            return $"http://{fallbackIp}:{ApiPort}/";
        }

        return $"http://{wifiIp}:{ApiPort}/";
    }
}