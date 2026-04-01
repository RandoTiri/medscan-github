using MedScan.Services;
using MedScan.Shared.Services;
using Microsoft.Extensions.Logging;
using MedScan.MAUI.Services;
using Plugin.LocalNotification;
using ZXing.Net.Maui.Controls;


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
                .UseLocalNotification()
                .ConfigureFonts(fonts => {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            builder.Services.AddSingleton<ITokenStore, MauiTokenStore>();

            builder.Services.AddSingleton(_ => new HttpClient {
                BaseAddress = new Uri(ApiBaseAddressProvider.GetApiBaseAddress())
            });


            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<IBarcodeScannerService, MauiBarcodeScannerService>();
            builder.Services.AddSingleton<IMedicationCatalogClient, MedicationCatalogClient>();
            builder.Services.AddSingleton<IScannerFlowService, ScannerFlowService>();

            builder.Services.AddScoped<IMedicineReminderScheduler,MauiMedicineReminderScheduler>();
            builder.Services.AddScoped<MedicineReminderCoordinator>();
            builder.Services.AddScoped<IMedicationService,ApiMedicationService>();


#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
