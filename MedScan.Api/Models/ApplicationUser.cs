using Microsoft.AspNetCore.Identity;

namespace MedScan.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}