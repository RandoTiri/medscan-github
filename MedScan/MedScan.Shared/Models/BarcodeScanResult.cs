namespace MedScan.Shared.Models;

public enum BarcodeScanStatus
{
    Success,
    PermissionDenied,
    NotDetected,
    Canceled,
    Error,
    ManualSearch
}

public sealed class BarcodeScanResult
{
    public BarcodeScanStatus Status { get; init; }
    public string? Barcode { get; init; }
    public string? Message { get; init; }
    public bool CanOpenSettings { get; init; }
}
