using MedScan.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedScan.Api.Controllers;

[ApiController]
[Route("api/medication-catalog")]
[Authorize]
public sealed class MedicationCatalogController(IMedicationCatalogService medicationCatalogService) : ControllerBase
{
    [HttpGet("by-barcode/{barcode}")]
    public async Task<IActionResult> FindByBarcode(string barcode)
    {
        var medication = await medicationCatalogService.FindByBarcodeAsync(barcode);
        if (medication is null)
        {
            return NotFound(new { message = "Tundmatu triipkood" });
        }

        return Ok(medication);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchByName([FromQuery] string query, [FromQuery] int limit = 20)
    {
        var items = await medicationCatalogService.SearchByNameAsync(query, limit);
        return Ok(items);
    }
}
