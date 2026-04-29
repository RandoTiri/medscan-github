using System.Security.Claims;
using MedScan.Api.Data;
using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
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

        var userMedication = await dbContext.UserMedications
            .Include(um => um.Profile)
            .Include(um => um.Medication)
            .Include(um => um.DoseLogs)
            .FirstOrDefaultAsync(um => um.Id == id && um.Profile.UserId == userId);

        if (userMedication is null)
        {
            return NotFound();
        }

        var nowLocal = DateTime.Now;
        var localDate = nowLocal.Date;
        var resolvedTime = dto.ScheduledTime ?? TimeOnly.FromDateTime(nowLocal);

        // DB column type is timestamptz; always persist ScheduledTime in UTC.
        var scheduledUtc = DateTime.SpecifyKind(localDate.Add(resolvedTime.ToTimeSpan()), DateTimeKind.Local)
            .ToUniversalTime();

        var existingLog = userMedication.DoseLogs
            .Where(log => log.ScheduledTime == scheduledUtc)
            .OrderByDescending(log => log.Id)
            .FirstOrDefault();

        var previousStatus = existingLog?.DoseStatus;
        if (existingLog is null)
        {
            existingLog = new DoseLog
            {
                UserMedicationId = userMedication.Id,
                ScheduledTime = scheduledUtc,
                DoseStatus = dto.Status,
                TakenAt = dto.Status == DoseStatusEnum.Done ? DateTime.UtcNow : null,
                ConfirmedByUserId = userId
            };

            dbContext.DoseLogs.Add(existingLog);
        }
        else
        {
            existingLog.DoseStatus = dto.Status;
            existingLog.TakenAt = dto.Status == DoseStatusEnum.Done ? DateTime.UtcNow : null;
            existingLog.ConfirmedByUserId = userId;
        }

        var stockWarning = string.Empty;
        int? remainingQuantity = null;
        var shouldDecreaseStock = dto.Status == DoseStatusEnum.Done && previousStatus != DoseStatusEnum.Done;
        if (shouldDecreaseStock)
        {
            var stockItem = await dbContext.HomePharmacyItems
                .Where(item => item.ProfileId == userMedication.ProfileId && item.MedicationId == userMedication.MedicationId)
                .OrderByDescending(item => item.AddedAt)
                .FirstOrDefaultAsync();

            if (stockItem is not null && stockItem.Quantity > 0)
            {
                if (stockItem.Quantity == 1)
                {
                    // Quantity constraint is > 0, so remove last pack row directly.
                    dbContext.HomePharmacyItems.Remove(stockItem);
                    remainingQuantity = 0;
                }
                else
                {
                    stockItem.Quantity -= 1;
                    remainingQuantity = stockItem.Quantity;
                }

                if (remainingQuantity <= 3)
                {
                    var medName = userMedication.Medication?.Name ?? "Ravim";
                    stockWarning = $"{medName}: alles on {remainingQuantity} tk. Vajadusel osta juurde, kui jätkad võtmist.";
                }
            }
        }

        await dbContext.SaveChangesAsync();

        var updated = await medicationService.GetByIdAsync(id);
        if (updated is null)
        {
            return NotFound();
        }

        updated.RemainingQuantity = remainingQuantity;
        updated.StockWarning = string.IsNullOrWhiteSpace(stockWarning) ? null : stockWarning;

        return Ok(updated);
    }
}
