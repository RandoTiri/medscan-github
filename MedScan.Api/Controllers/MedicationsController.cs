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

        var localStart = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Local);
        var localEnd = localStart.AddDays(1);
        var utcStart = localStart.ToUniversalTime();
        var utcEnd = localEnd.ToUniversalTime();

        var history = await dbContext.DoseLogs
            .AsNoTracking()
            .Include(log => log.UserMedication)
                .ThenInclude(um => um.Medication)
            .Where(log =>
                log.UserMedication.ProfileId == profileId &&
                log.ScheduledTime >= utcStart &&
                log.ScheduledTime < utcEnd)
            .OrderBy(log => log.ScheduledTime)
            .Select(log => new DoseHistoryItemDto
            {
                MedicationName = log.UserMedication.Medication.Name,
                Strength = log.UserMedication.Medication.StrengthMg,
                ScheduledTime = log.ScheduledTime,
                Status = log.DoseStatus
            })
            .ToListAsync();

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
    [HttpPost("{id:int}/take-once")]
    public async Task<ActionResult<TakeMedicationOnceResultDto>> TakeOnce(int id, [FromBody] TakeMedicationOnceDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (dto.ProfileId <= 0)
        {
            return BadRequest(new TakeMedicationOnceResultDto
            {
                Success = false,
                Message = "Profiil puudub."
            });
        }

        var quantityToTake = dto.Quantity <= 0 ? 1 : dto.Quantity;

        var ownsProfile = await dbContext.Profiles
            .AsNoTracking()
            .AnyAsync(p => p.Id == dto.ProfileId && p.UserId == userId);

        if (!ownsProfile)
        {
            return Forbid();
        }

        var medication = await dbContext.Medications
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (medication is null)
        {
            return NotFound(new TakeMedicationOnceResultDto
            {
                Success = false,
                Message = "Ravimit ei leitud."
            });
        }

        var stockItem = await dbContext.HomePharmacyItems
            .Where(item => item.ProfileId == dto.ProfileId && item.MedicationId == id)
            .OrderByDescending(item => item.AddedAt)
            .FirstOrDefaultAsync();

        if (stockItem is null || stockItem.Quantity <= 0)
        {
            return BadRequest(new TakeMedicationOnceResultDto
            {
                Success = false,
                Message = "Ravimit ei ole koduses varus."
            });
        }

        if (stockItem.Quantity == 1 && !dto.ConfirmLastUnit)
        {
            return Conflict(new TakeMedicationOnceResultDto
            {
                Success = false,
                RequiresLastUnitConfirmation = true,
                RemainingQuantity = 1,
                Message = "See on viimane ühik. Võtmisega jätkates pead uue paki ostma."
            });
        }

        if (stockItem.Quantity < quantityToTake)
        {
            return BadRequest(new TakeMedicationOnceResultDto
            {
                Success = false,
                Message = $"Kodus on alles {stockItem.Quantity} tk. Vähenda võetavat kogust."
            });
        }

        var takenAt = DateTime.UtcNow;
        var activeMedication = await dbContext.UserMedications
            .Where(um => um.ProfileId == dto.ProfileId && um.MedicationId == id && um.IsActive)
            .OrderByDescending(um => um.Id)
            .FirstOrDefaultAsync();

        var medicationForLog = activeMedication;
        if (medicationForLog is null)
        {
            medicationForLog = new UserMedication
            {
                ProfileId = dto.ProfileId,
                MedicationId = id,
                Frequency = 1,
                ScheduleUnit = MedicationScheduleUnit.Day,
                ScheduledTimesJson = "[\"08:00:00\"]",
                WeeklyDaysJson = "[]",
                StartDate = DateOnly.FromDateTime(DateTime.Now),
                RemindersEnabled = false,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? "Ühekordne võtmine" : dto.Notes.Trim(),
                IsActive = false,
                AddedAt = DateTime.UtcNow
            };

            dbContext.UserMedications.Add(medicationForLog);
            await dbContext.SaveChangesAsync();
        }

        dbContext.DoseLogs.Add(new DoseLog
        {
            UserMedicationId = medicationForLog.Id,
            ScheduledTime = takenAt,
            TakenAt = takenAt,
            DoseStatus = DoseStatusEnum.Done,
            ConfirmedByUserId = userId
        });

        var remaining = stockItem.Quantity - quantityToTake;
        var removed = remaining <= 0;
        if (removed)
        {
            dbContext.HomePharmacyItems.Remove(stockItem);
        }
        else
        {
            stockItem.Quantity = remaining;
        }

        await dbContext.SaveChangesAsync();

        return Ok(new TakeMedicationOnceResultDto
        {
            Success = true,
            RemainingQuantity = removed ? 0 : remaining,
            RemovedFromHomePharmacy = removed,
            Message = "Salvestatud logisse, meeldetuletust ei looda."
        });
    }
}




