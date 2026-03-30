using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MedScan.Services;

public static class ApiBaseAddressProvider
{
    private const int ApiPort = 5183;

    public static string GetApiBaseAddress()
    {
        var wifiIp = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni =>
                ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
                ni.OperationalStatus == OperationalStatus.Up)
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
            .Select(u => u.Address)
            .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

        if (wifiIp is null)
        {
            throw new InvalidOperationException("Active WiFi IPv4 address was not found.");
        }

        return $"http://{wifiIp}:{ApiPort}/";
    }
}
