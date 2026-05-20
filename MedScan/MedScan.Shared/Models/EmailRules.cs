using System.Net.Mail;

namespace MedScan.Shared.Models;

public static class EmailRules {
    public const string InvalidEmailMessage = "Invalid email.";

    public static bool IsValid(string? email) {
        if (string.IsNullOrWhiteSpace(email)) {
            return false;
        }

        return MailAddress.TryCreate(email.Trim(),out var address) &&
               string.Equals(address.Address,email.Trim(),StringComparison.OrdinalIgnoreCase);
    }
}