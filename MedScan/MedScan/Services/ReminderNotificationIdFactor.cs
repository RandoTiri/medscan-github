namespace MedScan.MAUI.Services;

public static class ReminderNotificationIdFactory {
    public static int Create(int userMedicationId,TimeOnly time) {
        return HashCode.Combine(userMedicationId,time.Hour,time.Minute) & int.MaxValue;
    }
}
