using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services;

namespace MedScan.MAUI.Services;

public sealed class MedicineReminderCoordinator
{
    private readonly IMedicineReminderScheduler _scheduler;

    public MedicineReminderCoordinator(IMedicineReminderScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public Task<bool> EnsurePermissionAsync()
    {
        return _scheduler.RequestPermissionAsync();
    }

    public Task ScheduleForMedicineAsync(UserMedicationDto medication)
    {
        var reminder = MedicineReminderMapper.ToReminderModel(medication);
        return _scheduler.ScheduleAsync(reminder);
    }

    public Task CancelForMedicineAsync(UserMedicationDto medication)
    {
        var reminder = MedicineReminderMapper.ToReminderModel(medication);
        return _scheduler.CancelAsync(reminder);
    }

    public Task RebuildAsync(IEnumerable<UserMedicationDto> medications)
    {
        var reminders = medications
            .Select(MedicineReminderMapper.ToReminderModel)
            .ToList();

        return _scheduler.RescheduleAllAsync(reminders);
    }

    public async Task SkipTodayDoseAsync(UserMedicationDto medication, TimeOnly scheduledTime)
    {
        if (!medication.RemindersEnabled)
        {
            return;
        }

        await _scheduler.CancelSingleAsync(medication.Id, scheduledTime);

        var reminder = MedicineReminderMapper.ToReminderModel(medication);
        var notifyTime = medication.ScheduleUnit == MedicationScheduleUnit.Day
            ? DateTime.Today.AddDays(1).Add(scheduledTime.ToTimeSpan())
            : GetNextOccurrence(reminder, scheduledTime, DateTime.Now.AddMinutes(1));

        if (notifyTime is not null)
        {
            await _scheduler.ScheduleSingleAsync(reminder, scheduledTime, notifyTime.Value);
        }
    }

    public async Task EnsureDoseFromNowAsync(UserMedicationDto medication, TimeOnly scheduledTime)
    {
        if (!medication.RemindersEnabled)
        {
            return;
        }

        await _scheduler.CancelSingleAsync(medication.Id, scheduledTime);

        var reminder = MedicineReminderMapper.ToReminderModel(medication);
        var notifyTime = medication.ScheduleUnit == MedicationScheduleUnit.Day
            ? GetNextDailyOccurrence(scheduledTime)
            : GetNextOccurrence(reminder, scheduledTime, DateTime.Now);

        if (notifyTime is not null)
        {
            await _scheduler.ScheduleSingleAsync(reminder, scheduledTime, notifyTime.Value);
        }
    }

    private static DateTime? GetNextOccurrence(MedicineReminderModel reminder, TimeOnly scheduledTime, DateTime fromLocal)
    {
        var index = reminder.ReminderTimes
            .Select((time, i) => new { time, i })
            .FirstOrDefault(x => x.time == scheduledTime)
            ?.i;

        if (index is null)
        {
            return null;
        }

        return MedicationScheduleCalculator.GetNextOccurrenceDateTime(
            reminder.ScheduleUnit,
            reminder.Frequency,
            reminder.StartDate,
            [reminder.ReminderTimes[index.Value]],
            reminder.ScheduleUnit == MedicationScheduleUnit.Week && reminder.WeeklyDays.Count > index.Value
                ? [reminder.WeeklyDays[index.Value]]
                : [],
            fromLocal);
    }

    private static DateTime GetNextDailyOccurrence(TimeOnly time)
    {
        var now = DateTime.Now;
        var scheduled = DateTime.Today.Add(time.ToTimeSpan());

        return scheduled > now
            ? scheduled
            : scheduled.AddDays(1);
    }
}
