namespace MedScan.Shared.Utilities;

public static class MedicationScanQueryParser {
    public static DateOnly? ParseExpiration(string? query) {
        if (string.IsNullOrWhiteSpace(query)) {
            return null;
        }

        return DateOnly.TryParse(query,out var parsed) ? parsed : null;
    }

    public static string? ParseBatch(string? query) {
        return string.IsNullOrWhiteSpace(query) ? null : query.Trim();
    }
}
