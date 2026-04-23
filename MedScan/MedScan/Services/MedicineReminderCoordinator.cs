using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Services;

namespace MedScan.MAUI.Services;

public sealed class MedicineReminderCoordinator {
    private readonly IMedicineReminderScheduler _scheduler;

    public MedicineReminderCoordinator(IMedicineReminderScheduler scheduler) {
        _scheduler = scheduler;
    }

    public Task<bool> EnsurePermissionAsync() {
        return _scheduler.RequestPermissionAsync();
    }

    public Task ScheduleForMedicineAsync(UserMedicationDto medication) {
        var reminder = MedicineReminderMapper.ToReminderModel(medication);
        return _scheduler.ScheduleAsync(reminder);
    }

    public Task CancelForMedicineAsync(UserMedicationDto medication) {
        var reminder = MedicineReminderMapper.ToReminderModel(medication);
        return _scheduler.CancelAsync(reminder);
    }

    public Task RebuildAsync(IEnumerable<UserMedicationDto> medications) {
        var reminders = medications
            .Select(MedicineReminderMapper.ToReminderModel)
            .ToList();

        return _scheduler.RescheduleAllAsync(reminders);
    }

    public async Task SkipTodayDoseAsync(UserMedicationDto medication, TimeOnly scheduledTime) {
        if (!medication.RemindersEnabled) {
            return;
        }

        await _scheduler.CancelSingleAsync(medication.Id, scheduledTime);

        var reminder = MedicineReminderMapper.ToReminderModel(medication);
        var tomorrow = DateTime.Today.AddDays(1).Add(scheduledTime.ToTimeSpan());
        await _scheduler.ScheduleSingleAsync(reminder, scheduledTime, tomorrow);
    }

    public async Task EnsureDoseFromNowAsync(UserMedicationDto medication, TimeOnly scheduledTime) {
        if (!medication.RemindersEnabled) {
            return;
        }

        await _scheduler.CancelSingleAsync(medication.Id, scheduledTime);

        var reminder = MedicineReminderMapper.ToReminderModel(medication);
        var notifyTime = GetNextOccurrence(scheduledTime);
        await _scheduler.ScheduleSingleAsync(reminder, scheduledTime, notifyTime);
    }

    private static DateTime GetNextOccurrence(TimeOnly time) {
        var now = DateTime.Now;
        var scheduled = DateTime.Today.Add(time.ToTimeSpan());

        return scheduled > now
            ? scheduled
            : scheduled.AddDays(1);
    }
}
