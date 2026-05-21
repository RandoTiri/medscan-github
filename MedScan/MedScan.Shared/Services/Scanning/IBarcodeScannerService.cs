using MedScan.Shared.Models;

namespace MedScan.Shared.Services.Scanning;

public interface IBarcodeScannerService {
    Task<BarcodeScanResult> ScanAsync(CancellationToken cancellationToken = default);
    Task OpenAppSettingsAsync();
}