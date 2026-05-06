using System.Security.Claims;
using APIDeliveryCRM.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShiftPlannerController : Controller
{
    private readonly IShiftPlannerService _planner;

    public ShiftPlannerController(IShiftPlannerService planner)
    {
        _planner = planner;
    }

    [HttpPost("rebuild")]
    [Authorize(Roles = "Логист,Логистика,Администратор,Админ,Менеджер")]
    public async Task<IActionResult> Rebuild([FromQuery] int? companyId, [FromQuery] string? reason, CancellationToken cancellationToken)
    {
        var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
        if (forbidden) return Forbid();
        if (!resolvedCompanyId.HasValue) return Unauthorized();

        var result = await _planner.RebuildCompanyPlanAsync(
            resolvedCompanyId.Value,
            string.IsNullOrWhiteSpace(reason) ? "manual.rebuild" : reason.Trim(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Логист,Логистика,Администратор,Админ,Менеджер")]
    public async Task<IActionResult> GetCurrent([FromQuery] int? companyId, CancellationToken cancellationToken)
    {
        var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
        if (forbidden) return Forbid();
        if (!resolvedCompanyId.HasValue) return Unauthorized();

        var result = await _planner.GetCompanyPlanAsync(resolvedCompanyId.Value, cancellationToken);
        return Ok(result);
    }

    [HttpGet("courier/{courierId:int}")]
    public async Task<IActionResult> GetCourierPlan(int courierId, CancellationToken cancellationToken)
    {
        var plan = await _planner.GetActivePlanForCourierAsync(courierId, cancellationToken);
        if (plan == null) return NotFound();

        var companyId = GetCompanyIdClaim();
        if (!companyId.HasValue || plan.CompanyId != companyId.Value)
            return Forbid();

        if (!IsStaff())
            return Forbid();

        return Ok(plan);
    }

    private bool IsStaff()
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToHashSet();
        return roles.Contains("Логист") || roles.Contains("Логистика") || roles.Contains("Администратор") || roles.Contains("Админ") || roles.Contains("Менеджер");
    }

    private int? GetCompanyIdClaim()
    {
        var raw = User.FindFirst("companyId")?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }

    private int? ResolveCompanyId(int? requestedCompanyId, out bool forbidden)
    {
        forbidden = false;
        var claimCompanyId = GetCompanyIdClaim();
        if (!claimCompanyId.HasValue)
            return null;

        if (requestedCompanyId.HasValue && requestedCompanyId.Value != claimCompanyId.Value)
        {
            forbidden = true;
            return null;
        }

        return claimCompanyId.Value;
    }
}
