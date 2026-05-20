using MedScan.MAUI.Pages;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services.Scanning;
using Microsoft.Extensions.Logging;

namespace MedScan.MAUI.Services.Scanning;

public sealed class MauiBarcodeScannerService(
    BarcodeScanFlowHandler scanFlowHandler,
    ILogger<BarcodeScannerPage> scannerPageLogger) : IBarcodeScannerService {
    public async Task<BarcodeScanResult> ScanAsync(CancellationToken cancellationToken = default) {
        var permissionStatus = await EnsureCameraPermissionAsync();
        if (permissionStatus != PermissionStatus.Granted) {
            return new BarcodeScanResult {
                Status = BarcodeScanStatus.PermissionDenied,
                Message = "Kaamera kasutamiseks on vaja luba.",
                CanOpenSettings = true
            };
        }

        var scannerPage = new BarcodeScannerPage(scanFlowHandler,scannerPageLogger);

        await PushModalOnMainThreadAsync(scannerPage);

        return await scannerPage.WaitForResultAsync(cancellationToken);
    }

    public Task OpenAppSettingsAsync() {
        AppInfo.ShowSettingsUI();
        return Task.CompletedTask;
    }

    private static Task PushModalOnMainThreadAsync(Page page) =>
        MainThread.InvokeOnMainThreadAsync(() => GetActiveNavigation().PushModalAsync(page));

    private static INavigation GetActiveNavigation() =>
        Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation
            ?? throw new InvalidOperationException("Skannerit ei saa avada, navigeerimine puudub.");

    private static async Task<PermissionStatus> EnsureCameraPermissionAsync() {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

        if (status == PermissionStatus.Granted) return status;

        return await Permissions.RequestAsync<Permissions.Camera>();
    }
}
