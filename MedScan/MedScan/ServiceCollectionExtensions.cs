using MedScan.MAUI.Services.Api;
using MedScan.MAUI.Services.Auth;
using MedScan.MAUI.Services.Notifications;
using MedScan.MAUI.Services.Platform;
using MedScan.MAUI.Services.Scanning;
using MedScan.MAUI.Services.Startup;
using MedScan.Shared.Services.Auth;
using MedScan.Shared.Services.Catalog;
using MedScan.Shared.Services.HomePharmacy;
using MedScan.Shared.Services.Medications;
using MedScan.Shared.Services.Notifications;
using MedScan.Shared.Services.Platform;
using MedScan.Shared.Services.Profiles;
using MedScan.Shared.Services.Scanning;

namespace MedScan.MAUI;

internal static class ServiceCollectionExtensions {
    public static IServiceCollection AddMedScanServices(this IServiceCollection services) {
        services
            .AddMedScanPlatformServices()
            .AddMedScanApiServices()
            .AddMedScanScanningServices()
            .AddMedScanNotificationServices()
            .AddMedScanStartupServices();

        services.AddMauiBlazorWebView();

        return services;
    }

    private static IServiceCollection AddMedScanPlatformServices(this IServiceCollection services) {
        services.AddSingleton<IFormFactor,FormFactor>();
        services.AddSingleton<ITokenStore,MauiTokenStore>();
        services.AddSingleton<IMedicationStatusEvents,MedicationStatusEvents>();
        services.AddScoped<IThemeService,ThemeService>();
        services.AddScoped<IExternalNavigationService,MauiExternalNavigationService>();

        return services;
    }

    private static IServiceCollection AddMedScanApiServices(this IServiceCollection services) {
        services.AddSingleton(_ => new HttpClient {
            BaseAddress = new Uri(ApiBaseAddressProvider.GetApiBaseAddress())
        });

        services.AddSingleton<IAuthService,AuthService>();
        services.AddSingleton<IMedicationCatalogClient,MedicationCatalogClient>();
        services.AddScoped<IMedicationService,ApiMedicationService>();
        services.AddScoped<IProfileService,ApiProfileService>();
        services.AddScoped<IHomePharmacyService,ApiHomePharmacyService>();

        return services;
    }

    private static IServiceCollection AddMedScanScanningServices(this IServiceCollection services) {
        services.AddSingleton<BarcodeScanFlowHandler>();
        services.AddSingleton<IBarcodeScannerService,MauiBarcodeScannerService>();
        services.AddSingleton<IScannerFlowService,ScannerFlowService>();

        return services;
    }

    private static IServiceCollection AddMedScanNotificationServices(this IServiceCollection services) {
        services.AddSingleton<IInAppDoseAlertService,InAppDoseAlertService>();
        services.AddSingleton<INotificationInboxService,LocalNotificationInboxService>();
        services.AddSingleton<DoseDueWatcherService>();
        services.AddScoped<IMedicineReminderScheduler,MauiMedicineReminderScheduler>();
        services.AddScoped<MedicineReminderCoordinator>();
        services.AddSingleton<NotificationDoseActionBridge>();

        return services;
    }

    private static IServiceCollection AddMedScanStartupServices(this IServiceCollection services) {
        services.AddScoped<AppStartupService>();

        return services;
    }
}
