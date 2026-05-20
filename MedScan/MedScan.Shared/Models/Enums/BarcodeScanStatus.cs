namespace MedScan.Shared.Models.Enums;

public enum BarcodeScanStatus {
    Success,
    PermissionDenied,
    NotDetected,
    Canceled,
    Error,
    ManualSearch
}