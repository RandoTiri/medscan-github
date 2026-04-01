using MedScan.Shared.Models;

namespace MedScan.Shared.Services;

public interface IBarcodeScannerService
{
    Task<BarcodeScanResult> ScanAsync(CancellationToken cancellationToken = default);
    Task OpenAppSettingsAsync();
}
