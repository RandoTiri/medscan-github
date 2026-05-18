using MedScan.Shared.Services;

namespace MedScan.MAUI.Services.Auth;

public sealed class MauiTokenStore : ITokenStore {
    private const string TokenKey = "auth_access_token";

    public Task<string?> GetTokenAsync() =>
        SecureStorage.Default.GetAsync(TokenKey);

    public Task SaveTokenAsync(string token) =>
        SecureStorage.Default.SetAsync(TokenKey, token);

    public Task RemoveTokenAsync() {
        SecureStorage.Default.Remove(TokenKey);
        return Task.CompletedTask;
    }
}