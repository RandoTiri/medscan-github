namespace MedScan.Shared.Services.Auth;

public interface ITokenStore {
    Task<string?> GetTokenAsync();
    Task SaveTokenAsync(string token);
    Task RemoveTokenAsync();
}