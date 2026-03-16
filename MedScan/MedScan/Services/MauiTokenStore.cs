using MedScan.Shared.Services;
using Microsoft.Maui.Storage;

namespace MedScan.Services;

public class MauiTokenStore : ITokenStore
{
    private const string TokenKey = "auth_access_token";

    public async Task<string?> GetTokenAsync()
    {
        return await SecureStorage.Default.GetAsync(TokenKey);
    }

    public async Task SaveTokenAsync(string token)
    {
        await SecureStorage.Default.SetAsync(TokenKey, token);
    }

    public Task RemoveTokenAsync()
    {
        SecureStorage.Default.Remove(TokenKey);
        return Task.CompletedTask;
    }
}