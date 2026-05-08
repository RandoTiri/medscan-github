namespace MedScan.Api.Services;

public interface IPasswordResetService
{
    Task<bool> SendResetCodeAsync(string email);
    bool VerifyCode(string email, string code);
    bool ConsumeCode(string email, string code);
}
