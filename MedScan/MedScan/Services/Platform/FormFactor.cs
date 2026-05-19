using MedScan.Shared.Services;

namespace MedScan.MAUI.Services.Platform; 
public sealed class FormFactor : IFormFactor {
    public string GetFormFactor() => 
        DeviceInfo.Idiom.ToString();

    public string GetPlatform() =>
        $"{DeviceInfo.Platform} - {DeviceInfo.VersionString}";
}