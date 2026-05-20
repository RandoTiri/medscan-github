using MedScan.Shared.Models.Enums;

namespace MedScan.Shared.Models;

public sealed class BarcodeScanResult {
    public BarcodeScanStatus Status { get; init; }
    public string? Barcode { get; init; }
    public DateOnly? ExpirationDate { get; init; }
    public string? BatchNumber { get; init; }
    public string? Message { get; init; }
    public bool CanOpenSettings { get; init; }
}