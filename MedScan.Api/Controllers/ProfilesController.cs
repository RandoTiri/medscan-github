using System.Security.Claims;
using MedScan.Api.Contracts;
using MedScan.Api.Data;
using MedScan.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ProfilesController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProfileSummaryResponse>> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var profile = await dbContext.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (profile is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(profile));
    }

    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<ProfileSummaryResponse>>> GetMine()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var profiles = await dbContext.Profiles
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.ProfileType == ProfileTypeEnum.Ise ? 0 : 1)
            .ThenBy(p => p.Id)
            .Select(p => new ProfileSummaryResponse
            {
                Id = p.Id,
                Name = p.Name,
                Gender = p.Gender,
                Type = p.ProfileType.ToString(),
                BirthDate = p.BirthDate
            })
            .ToListAsync();

        return Ok(profiles);
    }

    [HttpPost("patient")]
    public async Task<ActionResult<ProfileSummaryResponse>> CreatePatient([FromBody] CreatePatientProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Patsiendi nimi on kohustuslik." });
        }

        var profile = new MedScan.Shared.Models.Profile
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Gender = string.IsNullOrWhiteSpace(request.Gender) ? "Määramata" : request.Gender.Trim(),
            BirthDate = request.BirthDate,
            ProfileType = ProfileTypeEnum.Patsient
        };

        dbContext.Profiles.Add(profile);
        await dbContext.SaveChangesAsync();

        return Ok(ToResponse(profile));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProfileSummaryResponse>> UpdatePatient(int id, [FromBody] CreatePatientProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Patsiendi nimi on kohustuslik." });
        }

        var profile = await dbContext.Profiles
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (profile is null)
        {
            return NotFound();
        }

        if (profile.ProfileType == ProfileTypeEnum.Ise)
        {
            return BadRequest(new { message = "Põhiprofiili ei saa muuta sellelt vaatelt." });
        }

        profile.Name = request.Name.Trim();
        profile.Gender = string.IsNullOrWhiteSpace(request.Gender) ? "Määramata" : request.Gender.Trim();
        profile.BirthDate = request.BirthDate;

        await dbContext.SaveChangesAsync();
        return Ok(ToResponse(profile));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePatient(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var profile = await dbContext.Profiles
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (profile is null)
        {
            return NotFound();
        }

        if (profile.ProfileType == ProfileTypeEnum.Ise)
        {
            return BadRequest(new { message = "Põhiprofiili ei saa kustutada." });
        }

        dbContext.Profiles.Remove(profile);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    private static ProfileSummaryResponse ToResponse(MedScan.Shared.Models.Profile profile)
    {
        return new ProfileSummaryResponse
        {
            Id = profile.Id,
            Name = profile.Name,
            Gender = profile.Gender,
            Type = profile.ProfileType.ToString(),
            BirthDate = profile.BirthDate
        };
    }
}
