using MedScan.Pages;
using MedScan.Shared.Models;
using MedScan.Shared.Services;

namespace MedScan.Services;

public sealed class MauiBarcodeScannerService : IBarcodeScannerService
{
    public async Task<BarcodeScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var permissionStatus = await EnsureCameraPermissionAsync();
        if (permissionStatus != PermissionStatus.Granted)
        {
            return new BarcodeScanResult
            {
                Status = BarcodeScanStatus.PermissionDenied,
                Message = "Kaamera kasutamiseks on vaja luba.",
                CanOpenSettings = true
            };
        }

        var scannerPage = new BarcodeScannerPage();

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var navigation = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
            if (navigation is null)
            {
                throw new InvalidOperationException("Skannerit ei saa avada, navigeerimine puudub.");
            }

            await navigation.PushModalAsync(scannerPage);
        });

        return await scannerPage.WaitForResultAsync(cancellationToken);
    }

    public Task OpenAppSettingsAsync()
    {
        AppInfo.ShowSettingsUI();
        return Task.CompletedTask;
    }

    private static async Task<PermissionStatus> EnsureCameraPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

        if (status == PermissionStatus.Granted)
        {
            return status;
        }

        return await Permissions.RequestAsync<Permissions.Camera>();
    }
}
