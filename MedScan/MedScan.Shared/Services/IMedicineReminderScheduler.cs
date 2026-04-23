using MedScan.Shared.Models;

namespace MedScan.Shared.Services;

public interface IMedicineReminderScheduler {
    Task<bool> RequestPermissionAsync();
    Task ScheduleAsync(MedicineReminderModel medicine);
    Task CancelAsync(MedicineReminderModel medicine);
    Task ScheduleSingleAsync(MedicineReminderModel medicine, TimeOnly time, DateTime notifyTime);
    Task CancelSingleAsync(int userMedicationId, TimeOnly time);
    Task RescheduleAllAsync(IEnumerable<MedicineReminderModel> medicines);
}
