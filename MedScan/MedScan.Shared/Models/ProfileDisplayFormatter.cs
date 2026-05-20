namespace MedScan.Shared.Models;

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
}