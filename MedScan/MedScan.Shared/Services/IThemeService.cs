namespace MedScan.Shared.Services;

public interface IThemeService
{
    bool IsDarkMode { get; }
    bool IsInitialized { get; }
    event Action? OnChanged;
    Task InitializeAsync();
    Task SetDarkModeAsync(bool enabled);
}
