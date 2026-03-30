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
            return Results.BadRequest(new { message = "Vale email voi parool." });
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Results.BadRequest(new { message = "Vale email voi parool." });
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
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { message = "Koik valjad on kohustuslikud." });
        }

        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Results.BadRequest(new { message = "Selle emailiga kasutaja on juba olemas." });
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
}
