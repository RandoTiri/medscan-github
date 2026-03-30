using System.Security.Claims;
using MedScan.Api.Data;
using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MedicationsController(
    IMedicationService medicationService,
    AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserMedicationDto>>> GetSchedule([FromQuery] int profileId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var ownsProfile = await dbContext.Profiles
            .AsNoTracking()
            .AnyAsync(p => p.Id == profileId && p.UserId == userId);

        if (!ownsProfile)
        {
            return Forbid();
        }

        var result = await medicationService.GetScheduleAsync(profileId);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserMedicationDto>> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var ownsMedication = await dbContext.UserMedications
            .AsNoTracking()
            .AnyAsync(m => m.Id == id && m.Profile.UserId == userId);

        if (!ownsMedication)
        {
            return NotFound();
        }

        var result = await medicationService.GetByIdAsync(id);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserMedicationDto>> Add([FromBody] AddMedicationDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var ownsProfile = await dbContext.Profiles
            .AsNoTracking()
            .AnyAsync(p => p.Id == dto.ProfileId && p.UserId == userId);

        if (!ownsProfile)
        {
            return Forbid();
        }

        var result = await medicationService.AddToScheduleAsync(dto);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserMedicationDto>> Update(int id, [FromBody] AddMedicationDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var ownsMedication = await dbContext.UserMedications
            .AsNoTracking()
            .AnyAsync(m => m.Id == id && m.Profile.UserId == userId);

        if (!ownsMedication)
        {
            return NotFound();
        }

        var ownsTargetProfile = await dbContext.Profiles
            .AsNoTracking()
            .AnyAsync(p => p.Id == dto.ProfileId && p.UserId == userId);

        if (!ownsTargetProfile)
        {
            return Forbid();
        }

        var result = await medicationService.UpdateScheduleAsync(id, dto);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var ownsMedication = await dbContext.UserMedications
            .AsNoTracking()
            .AnyAsync(m => m.Id == id && m.Profile.UserId == userId);

        if (!ownsMedication)
        {
            return NotFound();
        }

        var removed = await medicationService.RemoveFromScheduleAsync(id);
        if (!removed)
        {
            return NotFound();
        }

        return NoContent();
    }
}
