using Microsoft.JSInterop;

namespace MedScan.Shared.Services;

public sealed class ThemeService(IJSRuntime jsRuntime) : IThemeService
{
    private const string ThemeStorageKey = "medscan.theme";
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public bool IsDarkMode { get; private set; }
    public bool IsInitialized { get; private set; }
    public event Action? OnChanged;

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        try
        {
            IsDarkMode = false;
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ThemeStorageKey, "light");
            await ApplyThemeAsync();
            IsInitialized = true;
            OnChanged?.Invoke();
        }
        catch
        {
            // Best effort. If JS isn't available yet, component can try again later.
        }
    }

    public async Task SetDarkModeAsync(bool enabled)
    {
        IsDarkMode = false;

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ThemeStorageKey, "light");
            await ApplyThemeAsync();
            IsInitialized = true;
        }
        catch
        {
            // Ignore JS failures to keep UI responsive.
        }

        OnChanged?.Invoke();
    }

    private Task ApplyThemeAsync()
    {
        var themeValue = IsDarkMode ? "dark" : "light";
        return _jsRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", themeValue).AsTask();
    }
}
