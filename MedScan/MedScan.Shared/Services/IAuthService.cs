using MedScan.Shared.Models;

namespace MedScan.Shared.Services;

public interface IAuthService {
    event Action? OnChange;

    bool IsLoggedIn { get; }
    bool IsInitialized { get; }
    UserInfo? CurrentUser { get; }

    Task InitializeAsync();
    Task<(bool Success,string ErrorMessage)> RegisterAsync(string fullName,string email,string password);
    Task<(bool Success,string ErrorMessage)> LoginAsync(string email,string password);
    Task<(bool Success,string ErrorMessage)> ForgotPasswordAsync(string email);
    Task<(bool Success,string ErrorMessage)> VerifyCodeAsync(string email, string code);
    Task<(bool Success,string ErrorMessage)> ResetPasswordAsync(string email, string code, string newPassword);
    Task LogoutAsync();
}