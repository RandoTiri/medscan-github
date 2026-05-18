using MedScan.Shared.Services;

namespace MedScan.Services.Platform {
    public class FormFactor : IFormFactor {
        public string GetFormFactor() {
            return DeviceInfo.Idiom.ToString();
        }

        public string GetPlatform() {
            return DeviceInfo.Platform.ToString() + " - " + DeviceInfo.VersionString;
        }
    }
}
