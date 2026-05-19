using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services;

namespace MedScan.MAUI.Services.Notifications;

public sealed class MedicineReminderCoordinator(IMedicineReminderScheduler scheduler) {
    public Task<bool> EnsurePermissionAsync() =>
        scheduler.RequestPermissionAsync();

    public Task ScheduleForMedicineAsync(UserMedicationDto medication) {
        var reminder = MedicineReminderMapper.ToReminderModel(medication);
        return scheduler.ScheduleAsync(reminder);
    }

    public Task CancelForMedicineAsync(UserMedicationDto medication) {
        var reminder = MedicineReminderMapper.ToReminderModel(medication);
        return scheduler.CancelAsync(reminder);
    }

    public Task RebuildAsync(IEnumerable<UserMedicationDto> medications) {
        var reminders = medications
            .Select(MedicineReminderMapper.ToReminderModel)
            .ToList();

        return scheduler.RescheduleAllAsync(reminders);
    }

    public async Task SkipTodayDoseAsync(UserMedicationDto medication,TimeOnly scheduledTime) {
        if (!medication.RemindersEnabled) 
            return;

        await scheduler.CancelSingleAsync(medication.Id,scheduledTime);

        var reminder = MedicineReminderMapper.ToReminderModel(medication);
        var notifyTime = medication.ScheduleUnit == MedicationScheduleUnit.Day
            ? DateTime.Today.AddDays(1).Add(scheduledTime.ToTimeSpan())
            : GetNextOccurrence(reminder,scheduledTime,DateTime.Now.AddMinutes(1));

        if (notifyTime is not null) 
            await scheduler.ScheduleSingleAsync(reminder,scheduledTime,notifyTime.Value);
    }

    public async Task EnsureDoseFromNowAsync(UserMedicationDto medication,TimeOnly scheduledTime) {
        if (!medication.RemindersEnabled) 
            return;

        await scheduler.CancelSingleAsync(medication.Id,scheduledTime);

        var reminder = MedicineReminderMapper.ToReminderModel(medication);
        var notifyTime = medication.ScheduleUnit == MedicationScheduleUnit.Day
            ? GetNextDailyOccurrence(scheduledTime)
            : GetNextOccurrence(reminder,scheduledTime,DateTime.Now);

        if (notifyTime is not null) 
            await scheduler.ScheduleSingleAsync(reminder,scheduledTime,notifyTime.Value);
    }

    private static DateTime? GetNextOccurrence(MedicineReminderModel reminder,TimeOnly scheduledTime,DateTime fromLocal) {
        var index = FindTimeIndex(reminder.ReminderTimes,scheduledTime);
        if (index < 0) {
            return null;
        }

        var weeklyDaysForOccurrence = reminder.ScheduleUnit == MedicationScheduleUnit.Week && reminder.WeeklyDays.Count > index
            ? new List<int> { reminder.WeeklyDays[index] }
            : [];

        return MedicationScheduleCalculator.GetNextOccurrenceDateTime(
            reminder.ScheduleUnit,
            reminder.Frequency,
            reminder.StartDate,
            [reminder.ReminderTimes[index]],
            weeklyDaysForOccurrence,
            fromLocal);
    }
    private static int FindTimeIndex(IReadOnlyList<TimeOnly> times,TimeOnly target) {
        for (var i = 0; i < times.Count; i++) {
            if (times[i] == target) 
                return i;
        }

        return -1;
    }

    private static DateTime GetNextDailyOccurrence(TimeOnly time) {
        var now = DateTime.Now;
        var scheduled = DateTime.Today.Add(time.ToTimeSpan());

        return scheduled > now
            ? scheduled
            : scheduled.AddDays(1);
    }
}