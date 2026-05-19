using MedScan.MAUI.Services.Startup;

namespace MedScan.MAUI;

public partial class App : Application
{
    public App(AppStartupService startupService)
    {
        InitializeComponent();
        startupService.Start();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "MedScan" };
    }
}