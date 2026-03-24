using MedScan.Shared.Models;

namespace MedScan.Shared.Services;

public interface IAuthService {
    bool IsLoggedIn { get; }
    bool IsInitialized { get; }
    UserInfo? CurrentUser { get; }

    Task InitializeAsync();
    Task<(bool Success,string ErrorMessage)> RegisterAsync(string fullName,string email,string password);
    Task<(bool Success,string ErrorMessage)> LoginAsync(string email,string password);
    Task LogoutAsync();
}