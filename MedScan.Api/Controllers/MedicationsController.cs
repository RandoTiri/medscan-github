using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedScan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MedicationsController : ControllerBase {
    private readonly IMedicationService _medicationService;

    public MedicationsController(IMedicationService medicationService) {
        _medicationService = medicationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserMedicationDto>>> GetSchedule([FromQuery] int profileId) {
        var result = await _medicationService.GetScheduleAsync(profileId);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserMedicationDto>> GetById(int id) {
        var result = await _medicationService.GetByIdAsync(id);
        if (result is null) {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserMedicationDto>> Add([FromBody] AddMedicationDto dto) {
        var result = await _medicationService.AddToScheduleAsync(dto);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserMedicationDto>> Update(int id,[FromBody] AddMedicationDto dto) {
        var result = await _medicationService.UpdateScheduleAsync(id,dto);
        if (result is null) {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id) {
        var removed = await _medicationService.RemoveFromScheduleAsync(id);
        if (!removed) {
            return NotFound();
        }

        return NoContent();
    }
}
