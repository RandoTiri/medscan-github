using System.Security.Claims;
using MedScan.Api.Repositories;
using MedScan.Api.Repositories.Profiles;
using MedScan.Shared.DTOs.HomePharmacy;
using MedScan.Shared.Services.HomePharmacy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedScan.Api.Controllers;

[ApiController]
[Route("api/home-pharmacy")]
[Authorize]
public sealed class HomePharmacyController(
    IHomePharmacyService homePharmacyService,
    IHomePharmacyRepository homePharmacyRepository,
    IProfileRepository profileRepository) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HomePharmacyItemDto>>> GetByProfile([FromQuery] int profileId) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        if (!await profileRepository.ExistsForUserAsync(profileId,userId)) {
            return Forbid();
        }

        var items = await homePharmacyService.GetByProfileIdAsync(profileId);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<HomePharmacyItemDto>> GetById(int id) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        if (!await homePharmacyRepository.IsOwnedByUserAsync(id,userId)) {
            return NotFound();
        }

        var item = await homePharmacyService.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<HomePharmacyItemDto>> Add([FromBody] AddHomePharmacyItemDto dto) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        if (!await profileRepository.ExistsForUserAsync(dto.ProfileId,userId)) {
            return Forbid();
        }

        HomePharmacyItemDto created;
        try {
            created = await homePharmacyService.AddAsync(dto);
        } catch (ArgumentException ex) {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<HomePharmacyItemDto>> Update(int id,[FromBody] UpdateHomePharmacyItemDto dto) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        if (!await homePharmacyRepository.IsOwnedByUserAsync(id,userId)) {
            return NotFound();
        }

        var updated = await homePharmacyService.UpdateAsync(id,dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id) {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        if (!await homePharmacyRepository.IsOwnedByUserAsync(id,userId)) {
            return NotFound();
        }

        var removed = await homePharmacyService.RemoveAsync(id);
        return removed ? NoContent() : NotFound();
    }
}