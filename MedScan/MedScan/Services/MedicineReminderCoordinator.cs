using MedScan.Shared.Models;
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

    public Task ScheduleForMedicineAsync(MedicineReminderModel medicine) {
        return _scheduler.ScheduleAsync(medicine);
    }

    public Task CancelForMedicineAsync(MedicineReminderModel medicine) {
        return _scheduler.CancelAsync(medicine);
    }

    public Task RebuildAsync(IEnumerable<MedicineReminderModel> medicines) {
        return _scheduler.RescheduleAllAsync(medicines);
    }
}
