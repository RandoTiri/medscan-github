using MedScan.MAUI.Services;
using MedScan.Services;
using MedScan.Shared.Services;
using System.Threading;

namespace MedScan;

public partial class App : Application
{
    private readonly IAuthService _authService;
    private readonly IMedicationService _medicationService;
    private readonly MedicineReminderCoordinator _reminderCoordinator;
    private readonly INotificationInboxService _notificationInboxService;
    private readonly SemaphoreSlim _reminderSync = new(1, 1);

    public App(
        IAuthService authService,
        IMedicationService medicationService,
        MedicineReminderCoordinator reminderCoordinator,
        NotificationDoseActionBridge notificationDoseActionBridge,
        INotificationInboxService notificationInboxService,
        DoseDueWatcherService doseDueWatcherService)
    {
        _authService = authService;
        _medicationService = medicationService;
        _reminderCoordinator = reminderCoordinator;
        _notificationInboxService = notificationInboxService;

        InitializeComponent();
        _ = notificationDoseActionBridge;
        doseDueWatcherService.EnsureStarted();
        _authService.OnChange += OnAuthStateChanged;
        _ = InitializeStartupAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "MedScan" };
    }

    private async Task InitializeStartupAsync()
    {
        try
        {
            await _notificationInboxService.InitializeAsync();
            await EnsureRemindersSyncedAsync();
        }
        catch
        {
            // Best effort: app should keep starting even if initialization fails.
        }
    }

    private void OnAuthStateChanged()
    {
        _ = EnsureRemindersSyncedAsync();
    }

    private async Task EnsureRemindersSyncedAsync()
    {
        await _reminderSync.WaitAsync();
        try
        {
            await _authService.InitializeAsync();

            var profileId = _authService.CurrentUser?.DefaultProfileId;
            if (!profileId.HasValue)
            {
                return;
            }

            var medications = await _medicationService.GetScheduleAsync(profileId.Value);
            await _reminderCoordinator.RebuildAsync(medications);
        }
        catch
        {
            // Best effort: reminders should not block app usage.
        }
        finally
        {
            _reminderSync.Release();
        }
    }
}
