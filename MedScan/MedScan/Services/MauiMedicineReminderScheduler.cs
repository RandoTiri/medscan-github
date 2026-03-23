using MedScan.Shared.Models;
using MedScan.Shared.Services;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace MedScan.MAUI.Services;

public sealed class MauiMedicineReminderScheduler : IMedicineReminderScheduler {
    public async Task<bool> RequestPermissionAsync() {
        var notificationsEnabled = await LocalNotificationCenter.Current.AreNotificationsEnabled();

        if (notificationsEnabled) {
            return true;
        }

        return await LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    public async Task ScheduleAsync(MedicineReminderModel medicine) {
        if (!medicine.RemindersEnabled || medicine.ReminderTimes.Count == 0) {
            return;
        }

        var permissionGranted = await RequestPermissionAsync();
        if (!permissionGranted) {
            return;
        }

        foreach (var time in medicine.ReminderTimes) {
            var notificationId = ReminderNotificationIdFactory.Create(medicine.UserMedicationId,time);

            var request = new NotificationRequest {
                NotificationId = notificationId,
                Title = "MedScan - aeg votta ravim",
                Description = BuildDescription(medicine),
                Schedule = new NotificationRequestSchedule {
                    NotifyTime = GetNextOccurrence(time),
                    RepeatType = NotificationRepeat.Daily
                }
            };

            await LocalNotificationCenter.Current.Show(request);
        }
    }

    public Task CancelAsync(MedicineReminderModel medicine) {
        foreach (var time in medicine.ReminderTimes) {
            var notificationId = ReminderNotificationIdFactory.Create(medicine.UserMedicationId,time);
            LocalNotificationCenter.Current.Cancel(notificationId);
        }

        return Task.CompletedTask;
    }

    public async Task RescheduleAllAsync(IEnumerable<MedicineReminderModel> medicines) {
        foreach (var medicine in medicines) {
            await CancelAsync(medicine);
            await ScheduleAsync(medicine);
        }
    }

    private static string BuildDescription(MedicineReminderModel medicine) {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(medicine.ProfileName)) {
            parts.Add(medicine.ProfileName);
        }

        if (!string.IsNullOrWhiteSpace(medicine.MedicationName)) {
            parts.Add(medicine.MedicationName);
        }

        if (!string.IsNullOrWhiteSpace(medicine.Dosage)) {
            parts.Add(medicine.Dosage);
        }

        return string.Join(" - ",parts);
    }

    private static DateTime GetNextOccurrence(TimeOnly time) {
        var now = DateTime.Now;
        var scheduled = DateTime.Today.Add(time.ToTimeSpan());

        return scheduled > now
            ? scheduled
            : scheduled.AddDays(1);
    }
}
