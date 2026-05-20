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
            var stored = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ThemeStorageKey);
            IsDarkMode = string.Equals(stored, "dark", StringComparison.OrdinalIgnoreCase);
            await ApplyThemeAsync();
            IsInitialized = true;
            OnChanged?.Invoke();
        }
        catch (Exception ex)
        {
            SharedDiagnostics.Log("THEME INIT", ex);
        }
    }

    public async Task SetDarkModeAsync(bool enabled)
    {
        //IsDarkMode = enabled;
        IsDarkMode = false;

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ThemeStorageKey, enabled ? "dark" : "light");
            await ApplyThemeAsync();
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            SharedDiagnostics.Log("THEME SET", ex);
        }

        OnChanged?.Invoke();
    }

    private Task ApplyThemeAsync()
    {
        var themeValue = IsDarkMode ? "dark" : "light";
        return _jsRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", themeValue).AsTask();
    }
}
