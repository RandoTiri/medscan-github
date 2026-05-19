using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using ZXing.Net.Maui.Controls;
using MedScan.MAUI.Services.Notifications;

namespace MedScan.MAUI
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

            builder.Services.AddMedScanServices();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
