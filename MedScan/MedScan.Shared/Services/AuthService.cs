using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MedScan.Shared.Models;

namespace MedScan.Shared.Services;

public class AuthService(HttpClient httpClient,ITokenStore tokenStore) : IAuthService {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private bool _isInitialized;
    public event Action? OnChange;
    public bool IsLoggedIn { get; private set; }
    public bool IsInitialized => _isInitialized;
    public UserInfo? CurrentUser { get; private set; }

    public async Task InitializeAsync() {
        if (_isInitialized) {
            return;
        }

        var token = await tokenStore.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token)) {
            _isInitialized = true;
            return;
        }

        SetAccessToken(token);
        CurrentUser = await GetCurrentUserAsync();
        IsLoggedIn = CurrentUser is not null;

        if (!IsLoggedIn) {
            await tokenStore.RemoveTokenAsync();
            SetAccessToken(null);
        }

        _isInitialized = true;
        NotifyStateChanged();
    }

    public async Task<(bool Success,string ErrorMessage)> RegisterAsync(string fullName,string email,string password,string gender,DateOnly? birthDate) {
        var baseAddress = httpClient.BaseAddress?.ToString() ?? "NULL";

        try {
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(gender) ||
                birthDate is null) {
                return (false,"Kõik väljad on kohustuslikud.");
            }

            var response = await httpClient.PostAsJsonAsync("/api/auth/register",new RegisterUserRequest {
                FullName = fullName.Trim(),
                Email = email.Trim(),
                Password = password,
                Gender = gender.Trim(),
                BirthDate = birthDate
            });

            if (!response.IsSuccessStatusCode) {
                return (false,await ApiErrorReader.ReadAsync(response));
            }

            return await LoginAsync(email,password);
        } catch (Exception ex) {
            AuthExceptionMessageBuilder.Log("REGISTER",baseAddress,ex);
            return (false,AuthExceptionMessageBuilder.Build("Registreerimine",ex));
        }
    }

    public async Task<(bool Success,string ErrorMessage)> LoginAsync(string email,string password) {
        var baseAddress = httpClient.BaseAddress?.ToString() ?? "NULL";

        try {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) {
                return (false,"Sisesta email ja parool.");
            }

            var response = await httpClient.PostAsJsonAsync("/api/auth/login",new LoginRequest {
                Email = email.Trim(),
                Password = password
            });

            if (!response.IsSuccessStatusCode) {
                return (false,await ApiErrorReader.ReadAsync(response));
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);

            if (loginResponse is null || string.IsNullOrWhiteSpace(loginResponse.AccessToken)) {
                return (false,"Sisselogimine ebaõnnestus.");
            }

            await tokenStore.SaveTokenAsync(loginResponse.AccessToken);
            SetAccessToken(loginResponse.AccessToken);

            CurrentUser = await GetCurrentUserAsync();
            IsLoggedIn = CurrentUser is not null;
            _isInitialized = true;
            NotifyStateChanged();

            if (!IsLoggedIn) {
                await tokenStore.RemoveTokenAsync();
                SetAccessToken(null);
                return (false,"Kasutaja andmete laadimine ebaõnnestus.");
            }

            return (true,string.Empty);
        } catch (Exception ex) {
            AuthExceptionMessageBuilder.Log("LOGIN",baseAddress,ex);
            return (false,AuthExceptionMessageBuilder.Build("Sisselogimine",ex));
        }
    }

    public async Task<(bool Success,string ErrorMessage)> ForgotPasswordAsync(string email) {
        var baseAddress = httpClient.BaseAddress?.ToString() ?? "NULL";

        try {
            if (string.IsNullOrWhiteSpace(email)) {
                return (false,"Sisesta email.");
            }

            var response = await httpClient.PostAsJsonAsync("/api/auth/forgot-password",new ForgotPasswordRequest {
                Email = email.Trim()
            });

            if (!response.IsSuccessStatusCode) {
                return (false,await ApiErrorReader.ReadAsync(response));
            }

            return (true,string.Empty);
        } catch (Exception ex) {
            AuthExceptionMessageBuilder.Log("FORGOT PASSWORD",baseAddress,ex);
            return (false,AuthExceptionMessageBuilder.Build("Parooli taastamine",ex));
        }
    }

    public async Task<(bool Success,string ErrorMessage)> VerifyCodeAsync(string email,string code) {
        var baseAddress = httpClient.BaseAddress?.ToString() ?? "NULL";

        try {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code)) {
                return (false,"Sisesta e-mail ja kood.");
            }

            var response = await httpClient.PostAsJsonAsync("/api/auth/verify-code",new VerifyCodeRequest {
                Email = email.Trim(),
                Code = code.Trim()
            });

            if (!response.IsSuccessStatusCode) {
                return (false,await ApiErrorReader.ReadAsync(response));
            }

            return (true,string.Empty);
        } catch (Exception ex) {
            AuthExceptionMessageBuilder.Log("VERIFY CODE",baseAddress,ex);
            return (false,AuthExceptionMessageBuilder.Build("Koodi kontroll",ex));
        }
    }

    public async Task<(bool Success,string ErrorMessage)> ResetPasswordAsync(string email,string code,string newPassword) {
        var baseAddress = httpClient.BaseAddress?.ToString() ?? "NULL";

        try {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(newPassword)) {
                return (false,"Kõik väljad on kohustuslikud.");
            }

            var response = await httpClient.PostAsJsonAsync("/api/auth/reset-password",new ResetPasswordRequest {
                Email = email.Trim(),
                Code = code.Trim(),
                NewPassword = newPassword
            });

            if (!response.IsSuccessStatusCode) {
                return (false,await ApiErrorReader.ReadAsync(response));
            }

            return (true,string.Empty);
        } catch (Exception ex) {
            AuthExceptionMessageBuilder.Log("RESET PASSWORD",baseAddress,ex);
            return (false,AuthExceptionMessageBuilder.Build("Parooli vahetamine",ex));
        }
    }

    public async Task<(bool Success,string ErrorMessage)> ChangePasswordAsync(string currentPassword,string newPassword) {
        var baseAddress = httpClient.BaseAddress?.ToString() ?? "NULL";

        try {
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword)) {
                return (false,"Kõik väljad on kohustuslikud.");
            }

            var response = await httpClient.PostAsJsonAsync("/api/auth/change-password",new ChangePasswordRequest {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            });

            if (!response.IsSuccessStatusCode) {
                return (false,await ApiErrorReader.ReadAsync(response));
            }

            return (true,string.Empty);
        } catch (Exception ex) {
            AuthExceptionMessageBuilder.Log("CHANGE PASSWORD",baseAddress,ex);
            return (false,AuthExceptionMessageBuilder.Build("Parooli muutmine",ex));
        }
    }

    public async Task<(bool Success,string ErrorMessage)> DeleteAccountAsync() {
        var baseAddress = httpClient.BaseAddress?.ToString() ?? "NULL";

        try {
            var response = await httpClient.DeleteAsync("/api/auth/me");

            if (!response.IsSuccessStatusCode) {
                return (false,await ApiErrorReader.ReadAsync(response));
            }

            CurrentUser = null;
            IsLoggedIn = false;
            _isInitialized = true;

            await tokenStore.RemoveTokenAsync();
            SetAccessToken(null);
            NotifyStateChanged();

            return (true,string.Empty);
        } catch (Exception ex) {
            AuthExceptionMessageBuilder.Log("DELETE ACCOUNT",baseAddress,ex);
            return (false,AuthExceptionMessageBuilder.Build("Konto kustutamine",ex));
        }
    }

    public async Task LogoutAsync() {
        CurrentUser = null;
        IsLoggedIn = false;
        _isInitialized = true;

        await tokenStore.RemoveTokenAsync();
        SetAccessToken(null);
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    private async Task<UserInfo?> GetCurrentUserAsync() {
        try {
            return await httpClient.GetFromJsonAsync<UserInfo>("/api/auth/me",JsonOptions);
        } catch {
            return null;
        }
    }

    private void SetAccessToken(string? token) {
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer",token);
    }
}