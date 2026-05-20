using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Utilities;

namespace MedScan.MAUI.Services.Notifications;

internal static class ReminderOccurrenceCalculator {
    public static DateTime GetNextDailyOccurrence(TimeOnly time,DateTime fromLocal) {
        var scheduled = fromLocal.Date.Add(time.ToTimeSpan());

        return scheduled > fromLocal
            ? scheduled
            : scheduled.AddDays(1);
    }

    public static DateTime GetNextDailyOccurrenceAfterToday(TimeOnly time,DateTime fromLocal) {
        return fromLocal.Date.AddDays(1).Add(time.ToTimeSpan());
    }

    public static DateTime? GetNextScheduledOccurrence(
        MedicineReminderModel reminder,
        TimeOnly scheduledTime,
        DateTime fromLocal) {
        if (reminder.ScheduleUnit == MedicationScheduleUnit.Day) {
            return GetNextDailyOccurrence(scheduledTime,fromLocal);
        }

        var timeIndex = FindTimeIndex(reminder.ReminderTimes,scheduledTime);
        if (timeIndex < 0) {
            return null;
        }

        var weeklyDaysForOccurrence = reminder.ScheduleUnit == MedicationScheduleUnit.Week &&
            reminder.WeeklyDays.Count > timeIndex
            ? new List<int> { reminder.WeeklyDays[timeIndex] }
            : [];

        return MedicationScheduleCalculator.GetNextOccurrenceDateTime(
            reminder.ScheduleUnit,
            reminder.Frequency,
            reminder.StartDate,
            [reminder.ReminderTimes[timeIndex]],
            weeklyDaysForOccurrence,
            fromLocal);
    }

    private static int FindTimeIndex(IReadOnlyList<TimeOnly> times,TimeOnly target) {
        for (var i = 0; i < times.Count; i++) {
            if (times[i] == target) {
                return i;
            }
        }

        return -1;
    }
}