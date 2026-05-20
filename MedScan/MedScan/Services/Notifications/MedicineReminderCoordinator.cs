using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services.Notifications;

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
            ? ReminderOccurrenceCalculator.GetNextDailyOccurrenceAfterToday(scheduledTime,DateTime.Now)
            : ReminderOccurrenceCalculator.GetNextScheduledOccurrence(reminder,scheduledTime,DateTime.Now.AddMinutes(1));

        if (notifyTime is not null) 
            await scheduler.ScheduleSingleAsync(reminder,scheduledTime,notifyTime.Value);
    }

    public async Task EnsureDoseFromNowAsync(UserMedicationDto medication,TimeOnly scheduledTime) {
        if (!medication.RemindersEnabled) 
            return;

        await scheduler.CancelSingleAsync(medication.Id,scheduledTime);

        var reminder = MedicineReminderMapper.ToReminderModel(medication);
        var notifyTime = ReminderOccurrenceCalculator.GetNextScheduledOccurrence(reminder,scheduledTime,DateTime.Now);

        if (notifyTime is not null) 
            await scheduler.ScheduleSingleAsync(reminder,scheduledTime,notifyTime.Value);
    }
}
