using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services.Notifications;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;

namespace MedScan.MAUI.Services.Notifications;

public sealed class MauiMedicineReminderScheduler : IMedicineReminderScheduler {
    public const int DoneActionId = 1001;
    public const string MedicationChannelId = "medscan.medication.reminders";

    public async Task<bool> RequestPermissionAsync() {
        var notificationsEnabled = await LocalNotificationCenter.Current.AreNotificationsEnabled();

        if (notificationsEnabled) 
            return true;

        return await LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    public async Task ScheduleAsync(MedicineReminderModel medicine) {
        if (!medicine.RemindersEnabled || medicine.ReminderTimes.Count == 0) 
            return;

        var permissionGranted = await RequestPermissionAsync();
        if (!permissionGranted) 
            return;

        if (medicine.ScheduleUnit == MedicationScheduleUnit.Day) {
            foreach (var time in medicine.ReminderTimes) {
                var notifyTime = ReminderOccurrenceCalculator.GetNextDailyOccurrence(time,DateTime.Now);
                await ScheduleSingleCoreAsync(medicine,time,notifyTime,NotificationRepeat.Daily);
            }
            return;
        }

        foreach (var time in medicine.ReminderTimes) {
            var notifyTime = ReminderOccurrenceCalculator.GetNextScheduledOccurrence(medicine,time,DateTime.Now);
            if (notifyTime is null) 
                continue;

            await ScheduleSingleCoreAsync(medicine,time,notifyTime.Value,NotificationRepeat.No);
        }
    }

    public Task CancelAsync(MedicineReminderModel medicine) {
        foreach (var time in medicine.ReminderTimes) {
            LocalNotificationCenter.Current.Cancel(ReminderNotificationIdFactory.Create(medicine.UserMedicationId,time));
        }
        return Task.CompletedTask;
    }

    public async Task ScheduleSingleAsync(MedicineReminderModel medicine,TimeOnly time,DateTime notifyTime) {
        if (!medicine.RemindersEnabled) 
            return;

        var permissionGranted = await RequestPermissionAsync();
        if (!permissionGranted) 
            return;

        var repeat = medicine.ScheduleUnit == MedicationScheduleUnit.Day
            ? NotificationRepeat.Daily
            : NotificationRepeat.No;

        await ScheduleSingleCoreAsync(medicine,time,notifyTime,repeat);
    }

    public Task CancelSingleAsync(int userMedicationId,TimeOnly time) {
        LocalNotificationCenter.Current.Cancel(ReminderNotificationIdFactory.Create(userMedicationId,time));
        return Task.CompletedTask;
    }

    public async Task RescheduleAllAsync(IEnumerable<MedicineReminderModel> medicines) {
        foreach (var medicine in medicines) {
            await CancelAsync(medicine);
            await ScheduleAsync(medicine);
        }
    }

    private static async Task ScheduleSingleCoreAsync(
        MedicineReminderModel medicine,
        TimeOnly time,
        DateTime notifyTime,
        NotificationRepeat repeatType) {
        RegisterActions();

        var request = new NotificationRequest {
            NotificationId = ReminderNotificationIdFactory.Create(medicine.UserMedicationId,time),
            Title = "MedScan - aeg võtta ravim",
            Description = BuildDescription(medicine),
            CategoryType = NotificationCategoryType.Status,
            ReturningData = ReminderPayloadCodec.Encode(
                medicine.UserMedicationId,
                time,
                medicine.MedicationName,
                medicine.ProfileName,
                note: null),
            Android = new AndroidOptions {
                ChannelId = MedicationChannelId,
                Priority = AndroidPriority.High,
                VisibilityType = AndroidVisibilityType.Public,
                AutoCancel = false,
                LaunchAppWhenTapped = true
            },
            Schedule = new NotificationRequestSchedule {
                NotifyTime = notifyTime,
                RepeatType = repeatType
            }
        };
        await LocalNotificationCenter.Current.Show(request);
    }

    private static void RegisterActions() {
        LocalNotificationCenter.Current.RegisterCategoryList(
        [
            new NotificationCategory(NotificationCategoryType.Status)
            {
                ActionList =
                [
                    new NotificationAction(DoneActionId)
                    {
                        Title = "Võetud",
                        Android = new AndroidAction
                        {
                            LaunchAppWhenTapped = false
                        }
                    }
                ]
            }
        ]);
    }

    private static string BuildDescription(MedicineReminderModel medicine) {
        var parts = new[] { medicine.ProfileName,medicine.MedicationName,medicine.Dosage }
            .Where(p => !string.IsNullOrWhiteSpace(p));

        return string.Join(" - ",parts);
    }

}
