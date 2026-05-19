using MedScan.MAUI.Services.Notifications;
using MedScan.Shared.Services;

namespace MedScan.MAUI.Services.Startup;

public sealed class AppStartupService(
    IAuthService authService,
    IMedicationService medicationService,
    MedicineReminderCoordinator reminderCoordinator,
    NotificationDoseActionBridge notificationDoseActionBridge,
    INotificationInboxService notificationInboxService,
    DoseDueWatcherService doseDueWatcherService) {
    private readonly SemaphoreSlim _reminderSync = new(1,1);
    private bool _isStarted;

    public void Start() {
        if (_isStarted) 
            return;

        _isStarted = true;
        _ = notificationDoseActionBridge;
        doseDueWatcherService.EnsureStarted();
        authService.OnChange += OnAuthStateChanged;
        _ = InitializeStartupAsync();
    }

    private async Task InitializeStartupAsync() {
        try {
            await notificationInboxService.InitializeAsync();
            await EnsureRemindersSyncedAsync();
        } catch { }
    }

    private void OnAuthStateChanged() {
        _ = EnsureRemindersSyncedAsync();
    }

    private async Task EnsureRemindersSyncedAsync() {
        await _reminderSync.WaitAsync();
        try {
            await authService.InitializeAsync();

            var profileId = authService.CurrentUser?.DefaultProfileId;
            if (!profileId.HasValue) {
                return;
            }

            var medications = await medicationService.GetScheduleAsync(profileId.Value);
            await reminderCoordinator.RebuildAsync(medications);
        } catch {
        } finally {
            _reminderSync.Release();
        }
    }
}