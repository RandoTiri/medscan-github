using System.Security.Claims;
using MedScan.Api.Data.Identity;
using MedScan.Api.Repositories;
using MedScan.Api.Repositories.Profiles;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MedScan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ProfilesController(
    IProfileRepository profileRepository,
    UserManager<ApplicationUser> userManager) : ControllerBase {
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProfileSummary>> GetById(int id) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        var profile = await profileRepository.GetForUserAsync(id,userId);
        if (profile is null) {
            return NotFound();
        }

        return Ok(ToResponse(profile));
    }

    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<ProfileSummary>>> GetMine() {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        var profiles = await profileRepository.GetAllForUserAsync(userId);
        return Ok(profiles.Select(ToResponse));
    }

    [HttpPost("patient")]
    public async Task<ActionResult<ProfileSummary>> CreatePatient([FromBody] CreatePatientProfileRequest request) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name)) {
            return BadRequest(new { message = "Patsiendi nimi on kohustuslik." });
        }

        var profile = new Profile {
            UserId = userId,
            Name = request.Name.Trim(),
            Gender = string.IsNullOrWhiteSpace(request.Gender) ? "Määramata" : request.Gender.Trim(),
            BirthDate = request.BirthDate,
            ProfileType = ProfileTypeEnum.Patsient
        };

        await profileRepository.AddAsync(profile);
        await profileRepository.SaveChangesAsync();

        return Ok(ToResponse(profile));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProfileSummary>> UpdatePatient(int id,[FromBody] CreatePatientProfileRequest request) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name)) {
            return BadRequest(new { message = "Patsiendi nimi on kohustuslik." });
        }

        var profile = await profileRepository.GetTrackedForUserAsync(id,userId);
        if (profile is null) {
            return NotFound();
        }

        profile.Name = request.Name.Trim();
        profile.Gender = string.IsNullOrWhiteSpace(request.Gender) ? "Määramata" : request.Gender.Trim();
        profile.BirthDate = request.BirthDate;

        if (profile.ProfileType == ProfileTypeEnum.Ise) {
            var user = await userManager.FindByIdAsync(userId);
            if (user is not null) {
                user.FullName = profile.Name;
                await userManager.UpdateAsync(user);
            }
        }

        await profileRepository.SaveChangesAsync();
        return Ok(ToResponse(profile));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePatient(int id) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        var profile = await profileRepository.GetTrackedForUserAsync(id,userId);
        if (profile is null) {
            return NotFound();
        }

        if (profile.ProfileType == ProfileTypeEnum.Ise) {
            return BadRequest(new { message = "Põhiprofiili ei saa kustutada." });
        }

        profileRepository.Remove(profile);
        await profileRepository.SaveChangesAsync();

        return NoContent();
    }

    private static ProfileSummary ToResponse(Profile profile) {
        return new ProfileSummary {
            Id = profile.Id,
            Name = profile.Name,
            Gender = profile.Gender,
            Type = profile.ProfileType.ToString(),
            BirthDate = profile.BirthDate
        };
    }
}