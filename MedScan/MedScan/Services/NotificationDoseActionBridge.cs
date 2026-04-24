using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace MedScan.MAUI.Services;

public sealed class NotificationDoseActionBridge : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IInAppDoseAlertService _inAppDoseAlertService;

    public NotificationDoseActionBridge(
        IServiceProvider serviceProvider,
        IInAppDoseAlertService inAppDoseAlertService)
    {
        _serviceProvider = serviceProvider;
        _inAppDoseAlertService = inAppDoseAlertService;
        LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationActionTapped;
    }

    public void Dispose()
    {
        LocalNotificationCenter.Current.NotificationActionTapped -= OnNotificationActionTapped;
    }

    private void OnNotificationActionTapped(Plugin.LocalNotification.EventArgs.NotificationActionEventArgs e)
    {
        _ = HandleActionAsync(e);
    }

    private async Task HandleActionAsync(Plugin.LocalNotification.EventArgs.NotificationActionEventArgs e)
    {
        if (!TryResolveStatus(e.ActionId, out var newStatus))
        {
            return;
        }

        var request = GetRequestFromEvent(e);
        if (request is null)
        {
            return;
        }

        if (!ReminderPayloadCodec.TryDecode(request.ReturningData, out var userMedicationId, out var scheduledTime))
        {
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var medicationService = scope.ServiceProvider.GetRequiredService<IMedicationService>();

            await medicationService.UpdateStatusAsync(userMedicationId, new UpdateMedicationStatusDto
            {
                ScheduledTime = scheduledTime,
                Status = newStatus
            });

            _inAppDoseAlertService.DismissByDose(userMedicationId, scheduledTime);

            // Remove handled notification from system notification center immediately.
            LocalNotificationCenter.Current.Clear([request.NotificationId]);
            LocalNotificationCenter.Current.Cancel([request.NotificationId]);
        }
        catch
        {
            // Best effort: notification actions must never crash the app process.
        }
    }

    private static NotificationRequest? GetRequestFromEvent(Plugin.LocalNotification.EventArgs.NotificationActionEventArgs e)
    {
        var prop = e.GetType().GetProperty("Request");
        return prop?.GetValue(e) as NotificationRequest;
    }

    private static bool TryResolveStatus(int actionId, out DoseStatusEnum status)
    {
        status = actionId switch
        {
            MauiMedicineReminderScheduler.DoneActionId => DoseStatusEnum.Done,
            _ => default
        };

        return actionId == MauiMedicineReminderScheduler.DoneActionId;
    }
}
