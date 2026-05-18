using Microsoft.AspNetCore.Identity;

namespace MedScan.Api.Data.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}