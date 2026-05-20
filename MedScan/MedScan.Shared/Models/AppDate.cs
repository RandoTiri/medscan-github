namespace MedScan.Shared.Models;

public static class AppDate {
    public static DateTime Now => DateTime.Now;
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    public static int CurrentYear => Today.Year;

    public static bool IsExpired(DateOnly? date) {
        return date is DateOnly value && value < Today;
    }

    public static int CalculateAge(DateOnly? birthDate) {
        if (!birthDate.HasValue) {
            return 0;
        }

        var today = Today;
        var age = today.Year - birthDate.Value.Year;

        if (today < birthDate.Value.AddYears(age)) {
            age--;
        }

        return Math.Max(age,0);
    }
}