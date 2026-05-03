using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Менеджер,Администратор,Админ")]
public class CompanySettingsController : Controller
{
    private readonly ICompanySettingsService _companySettingsService;

    public CompanySettingsController(ICompanySettingsService companySettingsService)
    {
        _companySettingsService = companySettingsService;
    }

    [HttpGet("sla")]
    public async Task<IActionResult> GetSla([FromQuery] int? companyId = null)
    {
        var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
        if (forbidden)
            return new ForbidResult();
        if (resolvedCompanyId <= 0)
            return new BadRequestObjectResult(new { message = "Не удалось определить компанию." });

        return await _companySettingsService.GetSlaSettingsAsync(resolvedCompanyId);
    }

    [HttpPut("sla")]
    public async Task<IActionResult> UpdateSla([FromBody] UpdateCompanySlaSettingsRequest request, [FromQuery] int? companyId = null)
    {
        var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
        if (forbidden)
            return new ForbidResult();
        if (resolvedCompanyId <= 0)
            return new BadRequestObjectResult(new { message = "Не удалось определить компанию." });

        return await _companySettingsService.UpdateSlaSettingsAsync(resolvedCompanyId, request);
    }

    private int ResolveCompanyId(int? requestedCompanyId, out bool forbidden)
    {
        forbidden = false;
        var claimCompanyId = GetCompanyIdFromClaims();
        if (!requestedCompanyId.HasValue)
            return claimCompanyId;
        if (IsAdmin())
            return requestedCompanyId.Value;
        if (claimCompanyId <= 0 || claimCompanyId != requestedCompanyId.Value)
        {
            forbidden = true;
            return 0;
        }
        return claimCompanyId;
    }

    private int GetCompanyIdFromClaims()
    {
        var raw = User.FindFirst("companyId")?.Value
                  ?? User.FindFirst(ClaimTypes.GroupSid)?.Value;
        return int.TryParse(raw, out var id) ? id : 0;
    }

    private bool IsAdmin()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        return string.Equals(role, "Админ", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "Администратор", System.StringComparison.OrdinalIgnoreCase);
    }
}

