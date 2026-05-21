using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MedScan.Shared.DTOs.Auth;
using MedScan.Shared.Services.Common;

namespace MedScan.Shared.Services.Auth;

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

    public Task<(bool Success,string ErrorMessage)> RegisterAsync(string fullName,string email,string password,string gender,DateOnly? birthDate) =>
        ExecuteAsync("REGISTER","Registreerimine",async () => {
            if (AnyMissing(fullName,email,password,gender) || birthDate is null) {
                return (false,"Kõik väljad on kohustuslikud.");
            }

            var result = await PostAsync("/api/auth/register",new RegisterUserRequest {
                FullName = fullName.Trim(),
                Email = email.Trim(),
                Password = password,
                Gender = gender.Trim(),
                BirthDate = birthDate
            });

            return result.Success ? await LoginAsync(email,password) : result;
        });

    public Task<(bool Success,string ErrorMessage)> LoginAsync(string email,string password) =>
        ExecuteAsync("LOGIN","Sisselogimine",async () => {
            if (AnyMissing(email,password)) {
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
        });

    public Task<(bool Success,string ErrorMessage)> ForgotPasswordAsync(string email) =>
        ExecuteAsync("FORGOT PASSWORD","Parooli taastamine",async () => {
            if (AnyMissing(email)) {
                return (false,"Sisesta email.");
            }

            return await PostAsync("/api/auth/forgot-password",new ForgotPasswordRequest {
                Email = email.Trim()
            });
        });

    public Task<(bool Success,string ErrorMessage)> VerifyCodeAsync(string email,string code) =>
        ExecuteAsync("VERIFY CODE","Koodi kontroll",async () => {
            if (AnyMissing(email,code)) {
                return (false,"Sisesta e-mail ja kood.");
            }

            return await PostAsync("/api/auth/verify-code",new VerifyCodeRequest {
                Email = email.Trim(),
                Code = code.Trim()
            });
        });

    public Task<(bool Success,string ErrorMessage)> ResetPasswordAsync(string email,string code,string newPassword) =>
        ExecuteAsync("RESET PASSWORD","Parooli vahetamine",async () => {
            if (AnyMissing(email,code,newPassword)) {
                return (false,"Kõik väljad on kohustuslikud.");
            }

            return await PostAsync("/api/auth/reset-password",new ResetPasswordRequest {
                Email = email.Trim(),
                Code = code.Trim(),
                NewPassword = newPassword
            });
        });

    public Task<(bool Success,string ErrorMessage)> ChangePasswordAsync(string currentPassword,string newPassword) =>
        ExecuteAsync("CHANGE PASSWORD","Parooli muutmine",async () => {
            if (AnyMissing(currentPassword,newPassword)) {
                return (false,"Kõik väljad on kohustuslikud.");
            }

            return await PostAsync("/api/auth/change-password",new ChangePasswordRequest {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            });
        });

    public Task<(bool Success,string ErrorMessage)> DeleteAccountAsync() =>
        ExecuteAsync("DELETE ACCOUNT","Konto kustutamine",async () => {
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
        });

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
        } catch (Exception ex) {
            SharedDiagnostics.Log("GET CURRENT USER",ex);
            return null;
        }
    }

    private void SetAccessToken(string? token) {
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer",token);
    }

    private async Task<(bool Success,string ErrorMessage)> PostAsync(string endpoint,object payload) {
        var response = await httpClient.PostAsJsonAsync(endpoint,payload);
        return response.IsSuccessStatusCode
            ? (true,string.Empty)
            : (false,await ApiErrorReader.ReadAsync(response));
    }

    private async Task<(bool Success,string ErrorMessage)> ExecuteAsync(
        string logTag,string userOperation,Func<Task<(bool Success,string ErrorMessage)>> action) {
        try {
            return await action();
        } catch (Exception ex) {
            AuthExceptionMessageBuilder.Log(logTag,httpClient.BaseAddress?.ToString() ?? "NULL",ex);
            return (false,AuthExceptionMessageBuilder.Build(userOperation,ex));
        }
    }

    private static bool AnyMissing(params string?[] values) {
        foreach (var value in values) {
            if (string.IsNullOrWhiteSpace(value)) {
                return true;
            }
        }
        return false;
    }
}