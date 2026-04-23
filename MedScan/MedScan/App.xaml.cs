using MedScan.MAUI.Services;
using MedScan.Shared.Services;

namespace MedScan;

public partial class App : Application
{
    public App(
        IAuthService authService,
        IMedicationService medicationService,
        MedicineReminderCoordinator reminderCoordinator,
        NotificationDoseActionBridge notificationDoseActionBridge)
    {
        InitializeComponent();
        _ = notificationDoseActionBridge;
        _ = InitializeRemindersAsync(authService, medicationService, reminderCoordinator);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "MedScan" };
    }

    private static async Task InitializeRemindersAsync(
        IAuthService authService,
        IMedicationService medicationService,
        MedicineReminderCoordinator reminderCoordinator)
    {
        try
        {
            await authService.InitializeAsync();

            var profileId = authService.CurrentUser?.DefaultProfileId;
            if (!profileId.HasValue)
            {
                return;
            }

            var medications = await medicationService.GetScheduleAsync(profileId.Value);
            await reminderCoordinator.RebuildAsync(medications);
        }
        catch
        {
            // Best effort: app should keep starting even if reminder restore fails.
        }
    }
}
