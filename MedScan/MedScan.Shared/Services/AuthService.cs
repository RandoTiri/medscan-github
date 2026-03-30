using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MedScan.Shared.Models;

namespace MedScan.Shared.Services;

public class AuthService(HttpClient httpClient, ITokenStore tokenStore)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private bool _isInitialized;

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
    }

    public async Task<(bool Success, string ErrorMessage)> RegisterAsync(string fullName, string email, string password)
    {
        var baseAddress = httpClient.BaseAddress?.ToString() ?? "NULL";

        try
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return (false, "Koik valjad on kohustuslikud.");
            }

            var response = await httpClient.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest
            {
                FullName = fullName.Trim(),
                Email = email.Trim(),
                Password = password
            });

            if (!response.IsSuccessStatusCode)
            {
                return (false, await ReadErrorMessageAsync(response));
            }

            return await LoginAsync(email, password);
        }
        catch (Exception ex)
        {
            return (false, $"REGISTER ERROR\nBaseAddress={baseAddress}\n{ex}");
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
            return (false, $"LOGIN ERROR\nBaseAddress={baseAddress}\n{ex}");
        }
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        IsLoggedIn = false;
        _isInitialized = true;

        await tokenStore.RemoveTokenAsync();
        SetAccessToken(null);
    }

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
}
