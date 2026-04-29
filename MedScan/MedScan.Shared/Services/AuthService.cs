using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MedScan.Shared.Models;

namespace MedScan.Shared.Services;

public class AuthService(HttpClient httpClient, ITokenStore tokenStore) : IAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private bool _isInitialized;

    public event Action? OnChange;

    public bool IsLoggedIn { get; private set; }
    public bool IsInitialized => _isInitialized;
    public UserInfo? CurrentUser { get; private set; }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        var token = await tokenStore.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
        {
            _isInitialized = true;
            return;
        }

        SetAccessToken(token);
        CurrentUser = await GetCurrentUserAsync();
        IsLoggedIn = CurrentUser is not null;

        if (!IsLoggedIn)
        {
            await tokenStore.RemoveTokenAsync();
            SetAccessToken(null);
        }

        _isInitialized = true;
        NotifyStateChanged();
    }

    public async Task<(bool Success, string ErrorMessage)> RegisterAsync(string fullName, string email, string password, string gender, DateOnly? birthDate)
    {
        var baseAddress = httpClient.BaseAddress?.ToString() ?? "NULL";

        try
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(gender) ||
                birthDate is null)
            {
                return (false, "Koik valjad on kohustuslikud.");
            }

            var response = await httpClient.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest
            {
                FullName = fullName.Trim(),
                Email = email.Trim(),
                Password = password,
                Gender = gender.Trim(),
                BirthDate = birthDate
            });

            if (!response.IsSuccessStatusCode)
            {
                return (false, await ReadErrorMessageAsync(response));
            }

            return await LoginAsync(email, password);
        }
        catch (Exception ex)
        {
            LogException("REGISTER", baseAddress, ex);
            return (false, BuildFriendlyExceptionMessage("Registreerimine", ex));
        }
    }

    public async Task<(bool Success, string ErrorMessage)> LoginAsync(string email, string password)
    {
        var baseAddress = httpClient.BaseAddress?.ToString() ?? "NULL";

        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "Sisesta email ja parool.");
            }

            var response = await httpClient.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Email = email.Trim(),
                Password = password
            });

            if (!response.IsSuccessStatusCode)
            {
                return (false, await ReadErrorMessageAsync(response));
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);

            if (loginResponse is null || string.IsNullOrWhiteSpace(loginResponse.AccessToken))
            {
                return (false, "Sisselogimine ebaonnestus.");
            }

            await tokenStore.SaveTokenAsync(loginResponse.AccessToken);
            SetAccessToken(loginResponse.AccessToken);

            CurrentUser = await GetCurrentUserAsync();
            IsLoggedIn = CurrentUser is not null;
            _isInitialized = true;
            NotifyStateChanged();

            if (!IsLoggedIn)
            {
                await tokenStore.RemoveTokenAsync();
                SetAccessToken(null);
                return (false, "Kasutaja andmete laadimine ebaonnestus.");
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            LogException("LOGIN", baseAddress, ex);
            return (false, BuildFriendlyExceptionMessage("Sisselogimine", ex));
        }
    }

    public async Task<(bool Success, string ErrorMessage)> ForgotPasswordAsync(string email)
    {
        var baseAddress = httpClient.BaseAddress?.ToString() ?? "NULL";

        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return (false, "Sisesta email.");
            }

            var response = await httpClient.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest
            {
                Email = email.Trim()
            });

            if (!response.IsSuccessStatusCode)
            {
                return (false, await ReadErrorMessageAsync(response));
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            LogException("FORGOT PASSWORD", baseAddress, ex);
            return (false, BuildFriendlyExceptionMessage("Parooli taastamine", ex));
        }
    }

    public async Task<(bool Success, string ErrorMessage)> VerifyCodeAsync(string email, string code)
    {
        var baseAddress = httpClient.BaseAddress?.ToString() ?? "NULL";

        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                return (false, "Sisesta e-mail ja kood.");
            }

            var response = await httpClient.PostAsJsonAsync("/api/auth/verify-code", new VerifyCodeRequest
            {
                Email = email.Trim(),
                Code = code.Trim()
            });

            if (!response.IsSuccessStatusCode)
            {
                return (false, await ReadErrorMessageAsync(response));
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            LogException("VERIFY CODE", baseAddress, ex);
            return (false, BuildFriendlyExceptionMessage("Koodi kontroll", ex));
        }
    }

    public async Task<(bool Success, string ErrorMessage)> ResetPasswordAsync(string email, string code, string newPassword)
    {
        var baseAddress = httpClient.BaseAddress?.ToString() ?? "NULL";

        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(newPassword))
            {
                return (false, "KÃµik vÃ¤ljad on kohustuslikud.");
            }

            var response = await httpClient.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest
            {
                Email = email.Trim(),
                Code = code.Trim(),
                NewPassword = newPassword
            });

            if (!response.IsSuccessStatusCode)
            {
                return (false, await ReadErrorMessageAsync(response));
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            LogException("RESET PASSWORD", baseAddress, ex);
            return (false, BuildFriendlyExceptionMessage("Parooli vahetamine", ex));
        }
    }

    public async Task<(bool Success, string ErrorMessage)> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var baseAddress = httpClient.BaseAddress?.ToString() ?? "NULL";

        try
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return (false, "KÃµik vÃ¤ljad on kohustuslikud.");
            }

            var response = await httpClient.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            });

            if (!response.IsSuccessStatusCode)
            {
                return (false, await ReadErrorMessageAsync(response));
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            LogException("CHANGE PASSWORD", baseAddress, ex);
            return (false, BuildFriendlyExceptionMessage("Parooli muutmine", ex));
        }
    }

    public async Task<(bool Success, string ErrorMessage)> DeleteAccountAsync()
    {
        var baseAddress = httpClient.BaseAddress?.ToString() ?? "NULL";

        try
        {
            var response = await httpClient.DeleteAsync("/api/auth/me");

            if (!response.IsSuccessStatusCode)
            {
                return (false, await ReadErrorMessageAsync(response));
            }

            CurrentUser = null;
            IsLoggedIn = false;
            _isInitialized = true;

            await tokenStore.RemoveTokenAsync();
            SetAccessToken(null);
            NotifyStateChanged();

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            LogException("DELETE ACCOUNT", baseAddress, ex);
            return (false, BuildFriendlyExceptionMessage("Konto kustutamine", ex));
        }
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        IsLoggedIn = false;
        _isInitialized = true;

        await tokenStore.RemoveTokenAsync();
        SetAccessToken(null);
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    private async Task<UserInfo?> GetCurrentUserAsync()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<UserInfo>("/api/auth/me", JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private void SetAccessToken(string? token)
    {
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(raw))
        {
            return "Toiming ebaonnestus.";
        }

        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("message", out var messageElement) &&
                    messageElement.ValueKind == JsonValueKind.String)
                {
                    return messageElement.GetString() ?? "Toiming ebaonnestus.";
                }

                if (root.TryGetProperty("title", out var titleElement) &&
                    titleElement.ValueKind == JsonValueKind.String)
                {
                    return titleElement.GetString() ?? "Toiming ebaonnestus.";
                }
            }

            if (root.ValueKind == JsonValueKind.Array)
            {
                var messages = root.EnumerateArray()
                    .Select(item =>
                    {
                        if (item.ValueKind == JsonValueKind.Object &&
                            item.TryGetProperty("description", out var descriptionElement) &&
                            descriptionElement.ValueKind == JsonValueKind.String)
                        {
                            return descriptionElement.GetString();
                        }

                        return item.ToString();
                    })
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .ToArray();

                var combined = string.Join(" ", messages);
                return string.IsNullOrWhiteSpace(combined) ? "Toiming ebaonnestus." : combined;
            }
        }
        catch
        {
        }

        return raw;
    }
    private static void LogException(string operation, string baseAddress, Exception ex)
    {
        Debug.WriteLine($"{operation} ERROR | BaseAddress={baseAddress} | {ex}");
    }

    private static string BuildFriendlyExceptionMessage(string operation, Exception ex)
    {
        if (ex is HttpRequestException)
        {
            return "Serveriga ei saadud Ã¼hendust. Kontrolli, et API tÃ¶Ã¶tab. USB Android testis tee ka adb reverse tcp:5183 tcp:5183.";
        }

        if (ex is TaskCanceledException)
        {
            return "PÃ¤ring aegus. Proovi uuesti.";
        }

        return $"{operation} ebaÃµnnestus. Proovi uuesti.";
    }
}


