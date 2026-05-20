namespace MedScan.Shared.Utilities;

public static class MedicationTextFormatter {
    public static string FormatPackSize(string? packSize) {
        if (string.IsNullOrWhiteSpace(packSize)) {
            return "-";
        }

        var digits = new string(packSize.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? packSize.Trim() : digits;
    }

    public static IEnumerable<string> SplitWarnings(string? warnings) {
        if (string.IsNullOrWhiteSpace(warnings)) {
            return [];
        }

        return warnings
            .Split(['\n',';','.'],StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(warning => warning.EndsWith('.') ? warning : $"{warning}.")
            .Where(warning => !string.IsNullOrWhiteSpace(warning));
    }
}