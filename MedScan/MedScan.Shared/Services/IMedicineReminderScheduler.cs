using MedScan.Shared.Models;

namespace MedScan.Shared.Services;

public interface IMedicineReminderScheduler {
    Task<bool> RequestPermissionAsync();
    Task ScheduleAsync(MedicineReminderModel medicine);
    Task CancelAsync(MedicineReminderModel medicine);
    Task RescheduleAllAsync(IEnumerable<MedicineReminderModel> medicines);
}
