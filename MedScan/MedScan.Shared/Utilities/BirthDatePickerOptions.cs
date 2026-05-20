namespace MedScan.Shared.Utilities;

public static class BirthDatePickerOptions {
    public static IReadOnlyList<int> Days { get; } = Enumerable.Range(1,31).ToList();
    public static IReadOnlyList<int> Months { get; } = Enumerable.Range(1,12).ToList();
    public static IReadOnlyList<int> Years { get; } = Enumerable.Range(1900,AppDate.CurrentYear - 1900 + 1).Reverse().ToList();

    public static void ApplyParts(DateOnly? birthDate,Action<string,string,string> applyParts) {
        if (birthDate is DateOnly date) {
            applyParts(date.Day.ToString(),date.Month.ToString(),date.Year.ToString());
            return;
        }

        applyParts(string.Empty,string.Empty,string.Empty);
    }

    public static bool TryClampDay(string? year,string? month,ref string day) {
        if (!int.TryParse(year,out var parsedYear) ||
            !int.TryParse(month,out var parsedMonth) ||
            !int.TryParse(day,out var parsedDay)) {
            return false;
        }

        var maxDay = DateTime.DaysInMonth(parsedYear,parsedMonth);
        if (parsedDay <= maxDay) {
            return false;
        }

        day = maxDay.ToString();
        return true;
    }
}
