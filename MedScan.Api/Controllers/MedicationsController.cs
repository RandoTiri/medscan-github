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
    public async Task<ActionResult<IEnumerable<UserMedicationDto>>> GetSchedule([FromQuery] int profileId, [FromQuery] DateOnly? forDate)
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

        var result = await medicationService.GetScheduleAsync(profileId, forDate);
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<DoseHistoryItemDto>>> GetHistory([FromQuery] int profileId, [FromQuery] DateOnly date)
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

        var history = await medicationService.GetHistoryAsync(profileId, date);
        return Ok(history);
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

    [HttpPost("{id:int}/status")]
    public async Task<ActionResult<UserMedicationDto>> UpdateStatus(int id, [FromBody] UpdateMedicationStatusDto dto)
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

        var updated = await medicationService.UpdateStatusAsync(id, dto);
        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    [HttpPost("{id:int}/take-once")]
    public async Task<ActionResult<TakeMedicationOnceResultDto>> TakeOnce(int id, [FromBody] TakeMedicationOnceDto dto)
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

        var result = await medicationService.TakeOnceAsync(id, dto);
        if (result.Success)
        {
            return Ok(result);
        }

        if (result.RequiresLastUnitConfirmation)
        {
            return Conflict(result);
        }

        if (result.Message == "Ravimit ei leitud.")
        {
            return NotFound(result);
        }

        return BadRequest(result);
    }
}
