using MedScan.Shared.Models.Enums;

namespace MedScan.Shared.Models;

public static class MedicationScheduleCalculator
{
    private const int MonthlyCycleDays = 30;

    public static List<MedicationScheduleOccurrence> GetOccurrencesForDate(
        MedicationScheduleUnit unit,
        int frequency,
        DateOnly startDate,
        IReadOnlyList<TimeOnly> scheduledTimes,
        IReadOnlyList<int>? weeklyDays,
        DateOnly selectedDate)
    {
        if (frequency <= 0 || scheduledTimes.Count == 0 || selectedDate < startDate)
        {
            return [];
        }

        return unit switch
        {
            MedicationScheduleUnit.Day => BuildDailyOccurrences(selectedDate, scheduledTimes),
            MedicationScheduleUnit.Week => BuildWeeklyOccurrences(startDate, selectedDate, scheduledTimes, weeklyDays),
            MedicationScheduleUnit.Month => BuildMonthlyOccurrences(frequency, startDate, selectedDate, scheduledTimes),
            _ => []
        };
    }

    public static DateTime? GetNextOccurrenceDateTime(
        MedicationScheduleUnit unit,
        int frequency,
        DateOnly startDate,
        IReadOnlyList<TimeOnly> scheduledTimes,
        IReadOnlyList<int>? weeklyDays,
        DateTime fromLocal)
    {
        if (frequency <= 0 || scheduledTimes.Count == 0)
        {
            return null;
        }

        var startSearchDate = DateOnly.FromDateTime(fromLocal);
        for (var i = 0; i < 370; i++)
        {
            var date = startSearchDate.AddDays(i);
            var occurrences = GetOccurrencesForDate(unit, frequency, startDate, scheduledTimes, weeklyDays, date)
                .OrderBy(o => o.Time)
                .ToList();

            foreach (var occurrence in occurrences)
            {
                var occurrenceDateTime = date.ToDateTime(occurrence.Time);
                if (occurrenceDateTime >= fromLocal)
                {
                    return occurrenceDateTime;
                }
            }
        }

        return null;
    }

    public static List<int> BuildMonthlyOffsets(int frequency)
    {
        if (frequency <= 0)
        {
            return [];
        }

        var offsets = new List<int>(frequency);
        for (var i = 0; i < frequency; i++)
        {
            offsets.Add((int)Math.Round(i * MonthlyCycleDays / (double)frequency, MidpointRounding.AwayFromZero));
        }

        return offsets;
    }

    private static List<MedicationScheduleOccurrence> BuildDailyOccurrences(
        DateOnly selectedDate,
        IReadOnlyList<TimeOnly> scheduledTimes)
    {
        return scheduledTimes
            .OrderBy(time => time)
            .Select((time, index) => new MedicationScheduleOccurrence(index, selectedDate, time))
            .ToList();
    }

    private static List<MedicationScheduleOccurrence> BuildWeeklyOccurrences(
        DateOnly startDate,
        DateOnly selectedDate,
        IReadOnlyList<TimeOnly> scheduledTimes,
        IReadOnlyList<int>? weeklyDays)
    {
        if (scheduledTimes.Count == 1)
        {
            var daysSinceStart = selectedDate.DayNumber - startDate.DayNumber;
            if (daysSinceStart >= 0 && daysSinceStart % 7 == 0)
            {
                return [new MedicationScheduleOccurrence(0, selectedDate, scheduledTimes[0])];
            }

            return [];
        }

        var selectedDay = (int)selectedDate.DayOfWeek;
        var resolvedDays = weeklyDays ?? [];
        var result = new List<MedicationScheduleOccurrence>();

        for (var i = 0; i < scheduledTimes.Count && i < resolvedDays.Count; i++)
        {
            if (resolvedDays[i] != selectedDay || selectedDate < startDate)
            {
                continue;
            }

            result.Add(new MedicationScheduleOccurrence(i, selectedDate, scheduledTimes[i]));
        }

        return result
            .OrderBy(occurrence => occurrence.Time)
            .ToList();
    }

    private static List<MedicationScheduleOccurrence> BuildMonthlyOccurrences(
        int frequency,
        DateOnly startDate,
        DateOnly selectedDate,
        IReadOnlyList<TimeOnly> scheduledTimes)
    {
        var result = new List<MedicationScheduleOccurrence>();
        var offsets = BuildMonthlyOffsets(frequency);
        var scheduledTime = scheduledTimes[0];

        for (var i = 0; i < offsets.Count; i++)
        {
            var firstOccurrence = startDate.AddDays(offsets[i]);
            var dayDelta = selectedDate.DayNumber - firstOccurrence.DayNumber;
            if (dayDelta < 0 || dayDelta % MonthlyCycleDays != 0)
            {
                continue;
            }

            result.Add(new MedicationScheduleOccurrence(i, selectedDate, scheduledTime));
        }

        return result
            .OrderBy(occurrence => occurrence.Time)
            .ToList();
    }
}
