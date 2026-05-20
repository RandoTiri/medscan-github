using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models.Enums;

namespace MedScan.Shared.Utilities;

public static class MedicationTextFormatter {
    public static string BuildDisplayName(UserMedicationDto medication) {
        return BuildDisplayName(medication.MedicationName,medication.Strength);
    }

    public static string BuildDisplayName(string? medicationName,string? strength) {
        var name = medicationName?.Trim() ?? string.Empty;
        var normalizedStrength = strength?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedStrength) ||
            name.Contains(normalizedStrength,StringComparison.OrdinalIgnoreCase)) {
            return name;
        }

        return $"{name} {normalizedStrength}";
    }

    public static string? NormalizeNote(string? note) {
        return string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

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

    public static string GetDoseStatusLabel(DoseStatusEnum? status) {
        return status switch {
            DoseStatusEnum.Done => "Võetud",
            DoseStatusEnum.Missed or DoseStatusEnum.Skipped => "Võtmata",
            _ => "Ootel"
        };
    }

    public static string GetDoseStatusCss(DoseStatusEnum? status) {
        return status switch {
            DoseStatusEnum.Done => "status-done",
            DoseStatusEnum.Missed or DoseStatusEnum.Skipped => "status-missed",
            _ => "status-pending"
        };
    }

    public static string GetDoseStatusBadgeCss(DoseStatusEnum? status) {
        return status switch {
            DoseStatusEnum.Done => "badge-done",
            DoseStatusEnum.Missed or DoseStatusEnum.Skipped => "badge-missed",
            _ => "badge-pending"
        };
    }

    public static string FormatSchedule(UserMedicationDto medication) {
        var unitSuffix = medication.ScheduleUnit switch {
            MedicationScheduleUnit.Day => "päevas",
            MedicationScheduleUnit.Week => "nädalas",
            MedicationScheduleUnit.Month => "kuus",
            _ => string.Empty
        };

        return $"{medication.FrequencyPerDay} korda {unitSuffix}";
    }

    public static string FormatShelfLife(DateOnly? expiresOn) {
        return expiresOn is DateOnly date
            ? $"Säilimisaeg: {date:dd.MM.yyyy}"
            : "Säilimisaeg puudub";
    }

    public static string FormatExpiration(DateOnly expiresOn) {
        var daysLeft = expiresOn.DayNumber - AppDate.Today.DayNumber;

        if (daysLeft >= 0 && daysLeft < 30) {
            return $"Aegub: {expiresOn:dd.MM.yyyy} (aegub {daysLeft} päeva pärast)";
        }

        return $"Aegub: {expiresOn:dd.MM.yyyy}";
    }

    public static string GetMedicationInitial(string? medicationName,string fallback = "R") {
        if (string.IsNullOrWhiteSpace(medicationName)) {
            return fallback;
        }

        var firstLetter = medicationName.Trim().FirstOrDefault(char.IsLetter);
        return firstLetter == default
            ? fallback
            : char.ToUpperInvariant(firstLetter).ToString();
    }
}