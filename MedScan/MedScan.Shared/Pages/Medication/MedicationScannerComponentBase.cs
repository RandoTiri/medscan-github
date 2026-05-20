using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services.Scanning;
using Microsoft.AspNetCore.Components;

namespace MedScan.Shared.Pages.Medication;

public abstract class MedicationScannerComponentBase : ComponentBase {
    [Inject] protected IScannerFlowService ScannerFlowService { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;

    protected virtual int? ScannerTargetProfileId => null;
    protected virtual bool ReplaceScannerNavigation => false;
    protected virtual string? CanceledScannerRoute => null;
    protected virtual bool NavigateOnManualSearch => true;

    protected async Task StartScannerAsync() {
        await RunMedicationScannerAsync(
            ScannerTargetProfileId,
            ReplaceScannerNavigation,
            CanceledScannerRoute,
            NavigateOnManualSearch);
    }

    protected async Task RunMedicationScannerAsync(
        int? targetProfileId = null,
        bool replace = false,
        string? canceledRoute = null,
        bool navigateOnManualSearch = true) {
        await OnBeforeScannerAsync();

        var scanResult = await ScannerFlowService.ScanAsync();

        switch (scanResult.Status) {
            case BarcodeScanStatus.Success when !string.IsNullOrWhiteSpace(scanResult.Barcode):
                await OnSuccessfulScanAsync(scanResult,targetProfileId,replace);
                break;
            case BarcodeScanStatus.ManualSearch when navigateOnManualSearch:
                await OnManualSearchScanAsync(targetProfileId,replace);
                break;
            case BarcodeScanStatus.Canceled when !string.IsNullOrWhiteSpace(canceledRoute):
                Navigation.NavigateTo(canceledRoute,replace);
                break;
            case BarcodeScanStatus.PermissionDenied:
                await OnPermissionDeniedScanAsync();
                break;
        }
    }

    protected virtual Task OnBeforeScannerAsync() {
        return Task.CompletedTask;
    }

    protected virtual Task OnSuccessfulScanAsync(BarcodeScanResult scanResult,int? targetProfileId,bool replace) {
        Navigation.NavigateTo(BuildSaveMedicationRoute(scanResult,targetProfileId),replace);
        return Task.CompletedTask;
    }

    protected virtual Task OnManualSearchScanAsync(int? targetProfileId,bool replace) {
        Navigation.NavigateTo(BuildManualSearchRoute(targetProfileId),replace);
        return Task.CompletedTask;
    }

    protected virtual Task OnPermissionDeniedScanAsync() {
        return Task.CompletedTask;
    }

    protected static string BuildSaveMedicationRoute(BarcodeScanResult scanResult,int? targetProfileId = null) {
        var route = $"/home-pharmacy/save-medication/{Uri.EscapeDataString(scanResult.Barcode ?? string.Empty)}";
        var query = new List<string>();

        if (targetProfileId is int profileId && profileId > 0) {
            query.Add($"profileId={profileId}");
        }

        if (scanResult.ExpirationDate is DateOnly expirationDate) {
            query.Add($"exp={Uri.EscapeDataString(expirationDate.ToString("yyyy-MM-dd"))}");
        }

        if (!string.IsNullOrWhiteSpace(scanResult.BatchNumber)) {
            query.Add($"lot={Uri.EscapeDataString(scanResult.BatchNumber)}");
        }

        return query.Count == 0
            ? route
            : $"{route}?{string.Join("&",query)}";
    }

    protected static string BuildManualSearchRoute(int? targetProfileId = null) {
        return targetProfileId is int profileId && profileId > 0
            ? $"/home-pharmacy/search?profileId={profileId}"
            : "/home-pharmacy/search";
    }
}
