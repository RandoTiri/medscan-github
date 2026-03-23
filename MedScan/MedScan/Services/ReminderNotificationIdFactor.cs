namespace MedScan.MAUI.Services;

public static class ReminderNotificationIdFactory {
    public static int Create(int medicationId,TimeOnly time) {
        return HashCode.Combine(medicationId,time.Hour,time.Minute) & int.MaxValue;
    }
}
