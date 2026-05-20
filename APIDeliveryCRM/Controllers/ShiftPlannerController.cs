using System.Security.Claims;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
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

    [HttpPost("courier/{courierId:int}/apply-route")]
    [Authorize(Roles = "Курьер,Логист,Логистика,Администратор,Админ,Менеджер")]
    public async Task<IActionResult> ApplyCourierRoute(int courierId, [FromBody] ApplyCourierRouteRequest request, CancellationToken cancellationToken)
    {
        var isCourier = User.IsInRole("Курьер");
        var isStaff = User.IsInRole("Логист") || User.IsInRole("Логистика")
            || User.IsInRole("Администратор") || User.IsInRole("Админ") || User.IsInRole("Менеджер");
        if (isCourier && !isStaff)
        {
            if (!await IsCourierSelfAsync(courierId))
                return Forbid();
        }
        else if (!isStaff)
        {
            return Forbid();
        }

        var companyId = GetCompanyIdClaim();
        if (!companyId.HasValue)
            return Unauthorized();

        if (request.Stops == null || request.Stops.Count == 0)
            return BadRequest(new { message = "Список точек маршрута пуст." });

        var reason = isCourier && !isStaff ? "courier.route_changed" : "logistician.route_map";
        var plan = await _planner.ApplyCourierRouteAsync(
            companyId.Value,
            courierId,
            request.Stops,
            reason,
            cancellationToken);
        if (plan == null)
            return BadRequest(new { message = "Не удалось применить маршрут. Проверьте координаты точек и заказы." });

        return Ok(plan);
    }

    [HttpGet("courier/{courierId:int}")]
    [Authorize(Roles = "Курьер,Логист,Логистика,Администратор,Админ,Менеджер")]
    public async Task<IActionResult> GetCourierPlan(int courierId, CancellationToken cancellationToken)
    {
        var plan = await _planner.GetCourierPlanAsync(courierId, cancellationToken);
        if (plan == null) return NotFound();

        var companyId = GetCompanyIdClaim();
        if (!companyId.HasValue || plan.CompanyId != companyId.Value)
            return Forbid();

        if (!IsStaff() && !await IsCourierSelfAsync(courierId))
            return Forbid();

        return Ok(plan);
    }

    private async Task<bool> IsCourierSelfAsync(int courierProfileId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userId, out var uid))
            return false;

        return await _planner.IsCourierOwnedByUserAsync(courierProfileId, uid);
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
