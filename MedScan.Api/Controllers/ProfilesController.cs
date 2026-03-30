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
                Type = p.ProfileType.ToString(),
                BirthDate = p.BirthDate
            })
            .ToListAsync();

        return Ok(profiles);
    }
}
