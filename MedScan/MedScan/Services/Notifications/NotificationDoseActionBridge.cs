using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.EventArgs;

namespace MedScan.MAUI.Services.Notifications;

public sealed class NotificationDoseActionBridge : IDisposable {
    private readonly IServiceProvider _serviceProvider;
    private readonly IInAppDoseAlertService _inAppDoseAlertService;
    private readonly ILogger<NotificationDoseActionBridge> _logger;

    public NotificationDoseActionBridge(
        IServiceProvider serviceProvider,
        IInAppDoseAlertService inAppDoseAlertService,
        ILogger<NotificationDoseActionBridge> logger) {
        _serviceProvider = serviceProvider;
        _inAppDoseAlertService = inAppDoseAlertService;
        _logger = logger;
        LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationActionTapped;
    }

    public void Dispose() {
        LocalNotificationCenter.Current.NotificationActionTapped -= OnNotificationActionTapped;
    }

    private void OnNotificationActionTapped(NotificationActionEventArgs e) {
        _ = HandleActionAsync(e);
    }

    private async Task HandleActionAsync(NotificationActionEventArgs e) {
        if (!TryResolveStatus(e.ActionId,out var newStatus)) 
            return;

        var request = GetRequestFromEvent(e);
        if (request is null) 
            return;

        if (!ReminderPayloadCodec.TryDecode(request.ReturningData,out var userMedicationId,out var scheduledTime)) 
            return;

        try {
            using var scope = _serviceProvider.CreateScope();
            var medicationService = scope.ServiceProvider.GetRequiredService<IMedicationService>();

            await medicationService.UpdateStatusAsync(userMedicationId,new UpdateMedicationStatusDto {
                ScheduledTime = scheduledTime,
                Status = newStatus
            });

            _inAppDoseAlertService.DismissByDose(userMedicationId,scheduledTime);

            LocalNotificationCenter.Current.Clear([request.NotificationId]);
            LocalNotificationCenter.Current.Cancel([request.NotificationId]);
        } catch (Exception ex) {
            _logger.LogWarning(ex,"Failed to handle notification action for medication {UserMedicationId}.",userMedicationId);
        }
    }

    private static NotificationRequest? GetRequestFromEvent(NotificationActionEventArgs e) {
        var prop = e.GetType().GetProperty("Request");
        return prop?.GetValue(e) as NotificationRequest;
    }

    private static bool TryResolveStatus(int actionId,out DoseStatusEnum status) {
        if (actionId == MauiMedicineReminderScheduler.DoneActionId) {
            status = DoseStatusEnum.Done;
            return true;
        }

        status = default;
        return false;
    }
}
