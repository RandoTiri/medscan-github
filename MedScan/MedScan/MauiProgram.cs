using MedScan.Services;
using MedScan.Shared.Services;
using Microsoft.Extensions.Logging;

namespace MedScan
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            builder.Services.AddSingleton<ITokenStore, MauiTokenStore>();
            builder.Services.AddSingleton(_ => new HttpClient
            {
                BaseAddress = new Uri(GetApiBaseAddress())
            });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<AuthService>();

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
