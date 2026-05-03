using System.Security.Claims;
using APIDeliveryCRM.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GeoAnalyticsController : Controller
{
    private readonly IGeoAnalyticsService _geoAnalyticsService;

    public GeoAnalyticsController(IGeoAnalyticsService geoAnalyticsService)
    {
        _geoAnalyticsService = geoAnalyticsService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, [FromQuery] double gridKm = 3.0)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
            return Unauthorized(new { message = "Company id was not found in token." });

        var now = DateTime.UtcNow;
        var from = fromUtc ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = toUtc ?? now;
        if (to < from)
            return BadRequest(new { message = "toUtc must be greater than or equal to fromUtc." });

        var dto = await _geoAnalyticsService.GetOverviewAsync(companyId.Value, from, to, gridKm);
        return Ok(dto);
    }

    private int? GetCompanyId()
    {
        var claimValue = User.FindFirst("companyId")?.Value
                         ?? User.FindFirst(ClaimTypes.GroupSid)?.Value;
        return int.TryParse(claimValue, out var companyId) ? companyId : null;
    }
}
