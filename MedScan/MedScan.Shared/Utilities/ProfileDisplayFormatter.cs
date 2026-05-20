using MedScan.Shared.Models;

namespace MedScan.Shared.Utilities;

public static class ProfileDisplayFormatter {
    public const string UnknownGender = "Määramata";
    public const string MissingAge = "Vanus puudub";

    public static string NormalizeGender(string? gender) {
        return string.IsNullOrWhiteSpace(gender) ? UnknownGender : gender;
    }

    public static string BuildDetails(Patient? patient) {
        if (patient is null) {
            return $"{MissingAge} · {UnknownGender}";
        }

        var age = patient.Age > 0 ? $"{patient.Age} a" : MissingAge;
        return $"{age} · {NormalizeGender(patient.Gender)}";
    }

    public static string BuildInitials(string? name,string fallback = "U") {
        if (string.IsNullOrWhiteSpace(name)) {
            return fallback;
        }

        var parts = name.Split(' ',StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) {
            return parts[0][..1].ToUpperInvariant();
        }

        return (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }
}
