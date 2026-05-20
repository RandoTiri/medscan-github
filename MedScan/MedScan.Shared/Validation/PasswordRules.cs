namespace MedScan.Shared.Validation;

public static class PasswordRules {
    public const int MinimumLength = 6;
    public const string RequirementsText = "Parool peab olema vähemalt 6 tähemärki pikk.";

    public static bool IsValid(string? password) {
        return !string.IsNullOrWhiteSpace(password) && password.Length >= MinimumLength;
    }
}
