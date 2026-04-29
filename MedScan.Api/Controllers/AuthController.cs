using MedScan.Api.Contracts;
using MedScan.Api.Data;
using MedScan.Api.Models;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AppDbContext dbContext) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { message = "Sisesta email ja parool." });
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Results.BadRequest(new { message = "Vale email vÃµi parool." });
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Results.BadRequest(new { message = "Vale email vÃµi parool." });
        }

        var principal = await signInManager.CreateUserPrincipalAsync(user);

        return TypedResults.SignIn(
            principal,
            authenticationScheme: IdentityConstants.BearerScheme);
    }

    [HttpPost("register")]
    public async Task<IResult> Register([FromBody] AppRegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Gender) ||
            request.BirthDate is null)
        {
            return Results.BadRequest(new { message = "Koik valjad on kohustuslikud." });
        }

        if (request.BirthDate > DateOnly.FromDateTime(DateTime.Today))
        {
            return Results.BadRequest(new { message = "Sunnikuupaev ei saa olla tulevikus." });
        }

        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Results.BadRequest(new { message = "Sellise emailiga kasutaja on juba olemas." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            EmailConfirmed = true
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return Results.BadRequest(result.Errors.Select(e => new
            {
                e.Code,
                e.Description
            }));
        }

        var defaultProfile = new Profile
        {
            UserId = user.Id,
            Name = user.FullName,
            Gender = request.Gender.Trim(),
            BirthDate = request.BirthDate,
            ProfileType = ProfileTypeEnum.Ise
        };

        dbContext.Profiles.Add(defaultProfile);
        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return Results.Ok(new
        {
            message = "User created",
            userId = user.Id,
            profileId = defaultProfile.Id
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IResult> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var defaultProfileId = await dbContext.Profiles
            .Where(p => p.UserId == user.Id)
            .OrderBy(p => p.ProfileType == ProfileTypeEnum.Ise ? 0 : 1)
            .ThenBy(p => p.Id)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();

        return Results.Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            defaultProfileId
        });
    }

    private static readonly Dictionary<string, string> _resetCodes = [];

    [HttpPost("forgot-password")]
    public async Task<IResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(new { message = "Email on kohustuslik!" });
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Results.BadRequest(new { message = "Antud emailiga ei ole selles rakenduses kontot!" });
        }

        var randomCode = new Random().Next(100000, 999999).ToString();
        _resetCodes[user.Email!] = randomCode;

        try
        {
            using var smtpClient = new MailKit.Net.Smtp.SmtpClient();
            await smtpClient.ConnectAsync("smtp.gmail.com",587,MailKit.Security.SecureSocketOptions.StartTls);
            await smtpClient.AuthenticateAsync("medscan.loputoo@gmail.com","iktwctxvuultzigq");

            var mailMessage = new MimeKit.MimeMessage();
            mailMessage.From.Add(new MimeKit.MailboxAddress("MedScan","medscan.loputoo@gmail.com"));
            mailMessage.To.Add(new MimeKit.MailboxAddress("",user.Email!));
            mailMessage.Subject = "MedScan kinnituskood";
            mailMessage.Body = new MimeKit.TextPart("plain") {
                Text = $"Tere! Teie kinnituskood on {randomCode}"
            };

            await smtpClient.SendAsync(mailMessage);
            await smtpClient.DisconnectAsync(true);
        }
        catch (Exception)
        {
            // E-maili saatmise viga
            return Results.BadRequest(new { message = "Kinnituskoodi saatmine ebaÃµnnestus. Kontrolli SMTP konfigureerimist." });
        }

        return Results.Ok(new { message = "Kood saadetud." });
    }

    [HttpPost("verify-code")]
    public IResult VerifyCode([FromBody] VerifyCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest(new { message = "Email ja kood on kohustuslikud." });
        }

        if (_resetCodes.TryGetValue(request.Email.Trim(), out var code) && code == request.Code.Trim())
        {
            return Results.Ok(new { message = "Kood on Ãµige." });
        }

        return Results.BadRequest(new { message = "Sisestatud kinnituskood on vale" });
    }

    [HttpPost("reset-password")]
    public async Task<IResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Results.BadRequest(new { message = "KÃµik vÃ¤ljad on kohustuslikud." });
        }

        if (request.NewPassword.Length < 6)
        {
            return Results.BadRequest(new { message = "Parool peab olema olema vÃ¤hemalt 6 tÃ¤hemÃ¤rki." });
        }

        if (!_resetCodes.TryGetValue(request.Email.Trim(), out var code) || code != request.Code.Trim())
        {
            return Results.BadRequest(new { message = "Kinnituskood on vale vÃµi aegunud." });
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Results.BadRequest(new { message = "Kasutajat ei leitud." });
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
        {
            return Results.BadRequest(result.Errors.Select(e => new
            {
                e.Code,
                e.Description
            }));
        }

        // Kood edukalt kasutatud, eemalda see
        _resetCodes.Remove(request.Email.Trim());

        return Results.Ok(new { message = "Parool edukalt uuendatud." });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IResult> ChangePassword([FromBody] MedScan.Api.Contracts.ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Results.BadRequest(new { message = "KÃµik vÃ¤ljad on kohustuslikud." });
        }

        if (request.NewPassword.Length < 6)
        {
            return Results.BadRequest(new { message = "Parool peab olema vÃ¤hemalt 6 tÃ¤hemÃ¤rki pikk." });
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return Results.BadRequest(result.Errors.Select(e => new
            {
                e.Code,
                e.Description
            }));
        }

        return Results.Ok(new { message = "Parool edukalt muudetud." });
    }

    [Authorize]
    [HttpDelete("me")]
    public async Task<IResult> DeleteAccount()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var userProfiles = await dbContext.Profiles
            .Where(p => p.UserId == user.Id)
            .ToListAsync();

        if (userProfiles.Count > 0)
        {
            dbContext.Profiles.RemoveRange(userProfiles);
            await dbContext.SaveChangesAsync();
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return Results.BadRequest(result.Errors.Select(e => new
            {
                e.Code,
                e.Description
            }));
        }

        await transaction.CommitAsync();

        return Results.NoContent();
    }
}
