using MedScan.Shared.Models;
using MedScan.Shared.Services;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;

namespace MedScan.MAUI.Services;

public sealed class MauiMedicineReminderScheduler : IMedicineReminderScheduler
{
    public const int DoneActionId = 1001;
    public const int MissedActionId = 1002;

    public async Task<bool> RequestPermissionAsync()
    {
        var notificationsEnabled = await LocalNotificationCenter.Current.AreNotificationsEnabled();

        if (notificationsEnabled)
        {
            return true;
        }

        return await LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    public async Task ScheduleAsync(MedicineReminderModel medicine)
    {
        if (!medicine.RemindersEnabled || medicine.ReminderTimes.Count == 0)
        {
            return;
        }

        var permissionGranted = await RequestPermissionAsync();
        if (!permissionGranted)
        {
            return;
        }

        foreach (var time in medicine.ReminderTimes)
        {
            await ScheduleSingleCoreAsync(medicine, time, GetNextOccurrence(time));
        }
    }

    public Task CancelAsync(MedicineReminderModel medicine)
    {
        foreach (var time in medicine.ReminderTimes)
        {
            LocalNotificationCenter.Current.Cancel(ReminderNotificationIdFactory.Create(medicine.UserMedicationId, time));
        }

        return Task.CompletedTask;
    }

    public async Task ScheduleSingleAsync(MedicineReminderModel medicine, TimeOnly time, DateTime notifyTime)
    {
        if (!medicine.RemindersEnabled)
        {
            return;
        }

        var permissionGranted = await RequestPermissionAsync();
        if (!permissionGranted)
        {
            return;
        }

        await ScheduleSingleCoreAsync(medicine, time, notifyTime);
    }

    public Task CancelSingleAsync(int userMedicationId, TimeOnly time)
    {
        LocalNotificationCenter.Current.Cancel(ReminderNotificationIdFactory.Create(userMedicationId, time));
        return Task.CompletedTask;
    }

    public async Task RescheduleAllAsync(IEnumerable<MedicineReminderModel> medicines)
    {
        foreach (var medicine in medicines)
        {
            await CancelAsync(medicine);
            await ScheduleAsync(medicine);
        }
    }

    private static async Task ScheduleSingleCoreAsync(MedicineReminderModel medicine, TimeOnly time, DateTime notifyTime)
    {
        RegisterActions();

        var request = new NotificationRequest
        {
            NotificationId = ReminderNotificationIdFactory.Create(medicine.UserMedicationId, time),
            Title = "MedScan - aeg votta ravim",
            Description = BuildDescription(medicine),
            CategoryType = NotificationCategoryType.Status,
            ReturningData = ReminderPayloadCodec.Encode(medicine.UserMedicationId, time),
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = notifyTime,
                RepeatType = NotificationRepeat.Daily
            }
        };

        await LocalNotificationCenter.Current.Show(request);
    }

    private static void RegisterActions()
    {
        LocalNotificationCenter.Current.RegisterCategoryList(
        [
            new NotificationCategory(NotificationCategoryType.Status)
            {
                ActionList =
                [
                    new NotificationAction(DoneActionId)
                    {
                        Title = "Tehtud",
                        Android = new AndroidAction
                        {
                            LaunchAppWhenTapped = false
                        }
                    },
                    new NotificationAction(MissedActionId)
                    {
                        Title = "Tegemata",
                        Android = new AndroidAction
                        {
                            LaunchAppWhenTapped = false
                        }
                    }
                ]
            }
        ]);
    }

    private static string BuildDescription(MedicineReminderModel medicine)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(medicine.ProfileName))
        {
            parts.Add(medicine.ProfileName);
        }

        if (!string.IsNullOrWhiteSpace(medicine.MedicationName))
        {
            parts.Add(medicine.MedicationName);
        }

        if (!string.IsNullOrWhiteSpace(medicine.Dosage))
        {
            parts.Add(medicine.Dosage);
        }

        return string.Join(" - ", parts);
    }

    private static DateTime GetNextOccurrence(TimeOnly time)
    {
        var now = DateTime.Now;
        var scheduled = DateTime.Today.Add(time.ToTimeSpan());

        return scheduled > now
            ? scheduled
            : scheduled.AddDays(1);
    }
}
