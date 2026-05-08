using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace MedScan.Api.Services;

public sealed class PasswordResetService(IOptions<SmtpOptions> smtpOptions) : IPasswordResetService
{
    private static readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresAtUtc)> ResetCodes = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(15);
    private readonly SmtpOptions _smtp = smtpOptions.Value;

    public async Task<bool> SendResetCodeAsync(string email)
    {
        var normalizedEmail = email.Trim();
        var code = Random.Shared.Next(100000, 999999).ToString();
        ResetCodes[normalizedEmail] = (code, DateTime.UtcNow.Add(CodeTtl));

        try
        {
            using var smtpClient = new MailKit.Net.Smtp.SmtpClient();
            var socketOptions = _smtp.UseStartTls
                ? MailKit.Security.SecureSocketOptions.StartTls
                : MailKit.Security.SecureSocketOptions.Auto;

            await smtpClient.ConnectAsync(_smtp.Host, _smtp.Port, socketOptions);
            await smtpClient.AuthenticateAsync(_smtp.UserName, _smtp.Password);

            var mailMessage = new MimeKit.MimeMessage();
            mailMessage.From.Add(new MimeKit.MailboxAddress(_smtp.FromName, _smtp.FromEmail));
            mailMessage.To.Add(new MimeKit.MailboxAddress(string.Empty, normalizedEmail));
            mailMessage.Subject = "MedScan kinnituskood";
            mailMessage.Body = new MimeKit.TextPart("plain")
            {
                Text = $"Tere! Teie kinnituskood on {code}"
            };

            await smtpClient.SendAsync(mailMessage);
            await smtpClient.DisconnectAsync(true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool VerifyCode(string email, string code)
    {
        var normalizedEmail = email.Trim();
        var normalizedCode = code.Trim();
        if (!ResetCodes.TryGetValue(normalizedEmail, out var entry))
        {
            return false;
        }

        if (entry.ExpiresAtUtc < DateTime.UtcNow)
        {
            ResetCodes.TryRemove(normalizedEmail, out _);
            return false;
        }

        return string.Equals(entry.Code, normalizedCode, StringComparison.Ordinal);
    }

    public bool ConsumeCode(string email, string code)
    {
        var normalizedEmail = email.Trim();
        if (!VerifyCode(normalizedEmail, code))
        {
            return false;
        }

        ResetCodes.TryRemove(normalizedEmail, out _);
        return true;
    }
}
