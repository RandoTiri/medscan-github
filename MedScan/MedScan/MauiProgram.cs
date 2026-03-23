using MedScan.Services;
using MedScan.Shared.Services;
using Microsoft.Extensions.Logging;
using MedScan.MAUI.Services;
using Plugin.LocalNotification;


namespace MedScan
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseLocalNotification()
                .ConfigureFonts(fonts => {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            builder.Services.AddSingleton<ITokenStore, MauiTokenStore>();

            builder.Services.AddSingleton(_ => new HttpClient {
                BaseAddress = new Uri(GetApiBaseAddress())
            });


            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<AuthService>();

            builder.Services.AddScoped<IMedicineReminderScheduler,MauiMedicineReminderScheduler>();
            builder.Services.AddScoped<MedicineReminderCoordinator>();
            builder.Services.AddScoped<IMedicationService,ApiMedicationService>();


#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static string GetApiBaseAddress()
        {
            return "http://172.20.10.5:5183/";
        }


    }
}
