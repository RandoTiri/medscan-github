namespace MedScan.Shared.Models;

public static class MedicationScheduleDefaults {
    public const int MinDailyFrequency = 1;
    public const int MaxDailyFrequency = 24;

    private static readonly int[] MondayBasedDays =
    [
        (int)DayOfWeek.Monday,
        (int)DayOfWeek.Tuesday,
        (int)DayOfWeek.Wednesday,
        (int)DayOfWeek.Thursday,
        (int)DayOfWeek.Friday,
        (int)DayOfWeek.Saturday,
        (int)DayOfWeek.Sunday
    ];

    public static List<string> BuildDefaultTimes(int frequency) {
        return frequency switch {
            1 => ["08:00"],
            2 => ["08:00","20:00"],
            3 => ["08:00","14:00","20:00"],
            4 => ["08:00","12:00","16:00","20:00"],
            _ => ["08:00"]
        };
    }

    public static List<int> BuildDefaultWeeklyDays(int frequency) {
        return MondayBasedDays.Take(frequency).ToList();
    }

    public static int GetMondayBasedWeekdayOrder(int day) {
        return day == (int)DayOfWeek.Sunday ? 6 : day - 1;
    }
}