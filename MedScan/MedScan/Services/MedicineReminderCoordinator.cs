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
}
