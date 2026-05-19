using MedScan.Shared.Models;
using MedScan.Shared.Services;
using ZXing.Net.Maui;

namespace MedScan.MAUI.Services.Scanning;

public sealed class BarcodeScanFlowHandler(IMedicationCatalogClient medicationCatalogClient) {
    public async Task<BarcodeScanFlowResult> HandleDetectedAsync(
        string rawValue,
        BarcodeFormat format,
        CancellationToken cancellationToken = default) {
        var barcodeValue = rawValue.Trim();
        if (string.IsNullOrWhiteSpace(barcodeValue)) {
            return BarcodeScanFlowResult.Ignore;
        }

        var lookupBarcode = barcodeValue;
        DateOnly? expirationDate = null;
        string? batchNumber = null;

        if (format == BarcodeFormat.DataMatrix &&
            Gs1DataMatrixParser.TryExtract(barcodeValue,out var parsed)) {
            lookupBarcode = parsed.LookupBarcode;
            expirationDate = parsed.ExpirationDate;
            batchNumber = parsed.BatchNumber;
        }

        try {
            var medication = await medicationCatalogClient.FindByBarcodeAsync(lookupBarcode,cancellationToken);
            if (medication is null) {
                return BarcodeScanFlowResult.NeedsPrompt(new BarcodeScanPrompt(
                    "Tundmatu triipkood",
                    "Tuvastatud triipkoodi andmeid ei leitud andmebaasist.",
                    "Skaneeri uuesti",
                    "Otsi käsitsi"));
            }
        } catch {
            return BarcodeScanFlowResult.NeedsPrompt(new BarcodeScanPrompt(
                "Viga",
                "Skaneerimisel tekkis tõrge.",
                "Skaneeri uuesti",
                "Otsi käsitsi"));
        }

        return BarcodeScanFlowResult.Completed(new BarcodeScanResult {
            Status = BarcodeScanStatus.Success,
            Barcode = lookupBarcode,
            ExpirationDate = expirationDate,
            BatchNumber = batchNumber
        });
    }
}

public sealed record BarcodeScanPrompt(
    string Title,
    string Message,
    string Accept,
    string Cancel);

public sealed record BarcodeScanFlowResult(
    BarcodeScanFlowResultKind Kind,
    BarcodeScanResult? Result,
    BarcodeScanPrompt? Prompt) {
    public static BarcodeScanFlowResult Ignore { get; } =
        new(BarcodeScanFlowResultKind.Ignore,null,null);

    public static BarcodeScanFlowResult Completed(BarcodeScanResult result) =>
        new(BarcodeScanFlowResultKind.Completed,result,null);

    public static BarcodeScanFlowResult NeedsPrompt(BarcodeScanPrompt prompt) =>
        new(BarcodeScanFlowResultKind.NeedsPrompt,null,prompt);
}

public enum BarcodeScanFlowResultKind {
    Ignore,
    Completed,
    NeedsPrompt
}