using MedScan.Shared.Services;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using ZXing.Net.Maui.Controls;
using MedScan.MAUI.Services.Api;
using MedScan.MAUI.Services.Auth;
using MedScan.MAUI.Services.Scanning;
using MedScan.MAUI.Services.Platform;
using MedScan.MAUI.Services.Notifications;


namespace MedScan
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader()
                .UseLocalNotification(config =>
                {
                    config.AddAndroid(android =>
                    {
                        android.AddChannel(new AndroidNotificationChannelRequest
                        {
                            Id = MauiMedicineReminderScheduler.MedicationChannelId,
                            Name = "Ravimi meeldetuletused",
                            Description = "Meeldetuletused ravimi võtmise kellaaegadel.",
                            Importance = AndroidImportance.High,
                            ShowBadge = true,
                            EnableSound = true,
                            EnableVibration = true
                        });
                    });
                })
                .ConfigureFonts(fonts => {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            builder.Services.AddSingleton<ITokenStore, MauiTokenStore>();
            builder.Services.AddSingleton<IMedicationStatusEvents, MedicationStatusEvents>();

            builder.Services.AddSingleton(_ => new HttpClient {
                BaseAddress = new Uri(ApiBaseAddressProvider.GetApiBaseAddress())
            });


            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IBarcodeScannerService, MauiBarcodeScannerService>();
            builder.Services.AddSingleton<IMedicationCatalogClient, MedicationCatalogClient>();
            builder.Services.AddSingleton<IScannerFlowService, ScannerFlowService>();
            builder.Services.AddSingleton<IInAppDoseAlertService, InAppDoseAlertService>();
            builder.Services.AddSingleton<INotificationInboxService, LocalNotificationInboxService>();
            builder.Services.AddSingleton<DoseDueWatcherService>();
            builder.Services.AddScoped<IThemeService, ThemeService>();
            builder.Services.AddScoped<IExternalNavigationService, MauiExternalNavigationService>();

            builder.Services.AddScoped<IMedicineReminderScheduler,MauiMedicineReminderScheduler>();
            builder.Services.AddScoped<MedicineReminderCoordinator>();
            builder.Services.AddSingleton<NotificationDoseActionBridge>();
            builder.Services.AddScoped<IMedicationService,ApiMedicationService>();
            builder.Services.AddScoped<IProfileService, ApiProfileService>();
            builder.Services.AddScoped<IHomePharmacyService, ApiHomePharmacyService>();


#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

