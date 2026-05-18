using System.Security.Claims;
using MedScan.Api.Repositories;
using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedScan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MedicationsController(
    IMedicationService medicationService,
    IProfileRepository profileRepository,
    IUserMedicationRepository userMedicationRepository) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserMedicationDto>>> GetSchedule([FromQuery] int profileId,[FromQuery] DateOnly? forDate) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        if (!await profileRepository.ExistsForUserAsync(profileId,userId)) {
            return Forbid();
        }

        var result = await medicationService.GetScheduleAsync(profileId,forDate);
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<DoseHistoryItemDto>>> GetHistory([FromQuery] int profileId,[FromQuery] DateOnly date) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        if (!await profileRepository.ExistsForUserAsync(profileId,userId)) {
            return Forbid();
        }

        var history = await medicationService.GetHistoryAsync(profileId,date);
        return Ok(history);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserMedicationDto>> GetById(int id) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        if (!await userMedicationRepository.IsOwnedByUserAsync(id,userId)) {
            return NotFound();
        }

        var result = await medicationService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserMedicationDto>> Add([FromBody] AddMedicationDto dto) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        if (!await profileRepository.ExistsForUserAsync(dto.ProfileId,userId)) {
            return Forbid();
        }

        var result = await medicationService.AddToScheduleAsync(dto);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserMedicationDto>> Update(int id,[FromBody] AddMedicationDto dto) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        if (!await userMedicationRepository.IsOwnedByUserAsync(id,userId)) {
            return NotFound();
        }

        if (!await profileRepository.ExistsForUserAsync(dto.ProfileId,userId)) {
            return Forbid();
        }

        var result = await medicationService.UpdateScheduleAsync(id,dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        if (!await userMedicationRepository.IsOwnedByUserAsync(id,userId)) {
            return NotFound();
        }

        var removed = await medicationService.RemoveFromScheduleAsync(id);
        return removed ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/status")]
    public async Task<ActionResult<UserMedicationDto>> UpdateStatus(int id,[FromBody] UpdateMedicationStatusDto dto) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        if (!await userMedicationRepository.IsOwnedByUserAsync(id,userId)) {
            return NotFound();
        }

        var updated = await medicationService.UpdateStatusAsync(id,dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPost("{id:int}/take-once")]
    public async Task<ActionResult<TakeMedicationOnceResultDto>> TakeOnce(int id,[FromBody] TakeMedicationOnceDto dto) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        if (!await profileRepository.ExistsForUserAsync(dto.ProfileId,userId)) {
            return Forbid();
        }

        var result = await medicationService.TakeOnceAsync(id,dto);
        if (result.Success) {
            return Ok(result);
        }

        if (result.RequiresLastUnitConfirmation) {
            return Conflict(result);
        }

        if (result.Message == "Ravimit ei leitud.") {
            return NotFound(result);
        }

        return BadRequest(result);
    }
}