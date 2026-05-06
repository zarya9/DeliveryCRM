using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeesController : Controller
    {
        private readonly IEmployeeService _employeeService;

        public EmployeesController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpGet]
        public Task<IActionResult> GetByCompany([FromQuery] int? companyId = null)
        {
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Task.FromResult<IActionResult>(Forbid());
            if (!resolvedCompanyId.HasValue) return Task.FromResult<IActionResult>(Unauthorized());
            return _employeeService.GetByCompanyAsync(resolvedCompanyId.Value);
        }

        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateEmployeeRequest request, [FromQuery] int? companyId = null)
        {
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Task.FromResult<IActionResult>(Forbid());
            if (!resolvedCompanyId.HasValue) return Task.FromResult<IActionResult>(Unauthorized());
            return _employeeService.CreateAsync(request, resolvedCompanyId.Value);
        }

        [HttpPost("{employeeId:int}/fire")]
        [Authorize(Roles = "Менеджер,Администратор,Админ")]
        public Task<IActionResult> FireEmployee(int employeeId, [FromQuery] int? companyId = null)
        {
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Task.FromResult<IActionResult>(Forbid());
            if (!resolvedCompanyId.HasValue) return Task.FromResult<IActionResult>(Unauthorized());
            var actorUserId = GetCurrentUserId();
            if (!actorUserId.HasValue) return Task.FromResult<IActionResult>(Unauthorized());
            return _employeeService.FireAsync(employeeId, resolvedCompanyId.Value, actorUserId.Value);
        }

        [HttpPost("{employeeId:int}/role")]
        [Authorize(Roles = "Менеджер,Администратор,Админ")]
        public Task<IActionResult> ChangeEmployeeRole(int employeeId, [FromBody] ChangeEmployeeRoleRequest request, [FromQuery] int? companyId = null)
        {
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Task.FromResult<IActionResult>(Forbid());
            if (!resolvedCompanyId.HasValue) return Task.FromResult<IActionResult>(Unauthorized());
            var actorUserId = GetCurrentUserId();
            if (!actorUserId.HasValue) return Task.FromResult<IActionResult>(Unauthorized());
            return _employeeService.ChangeRoleAsync(employeeId, resolvedCompanyId.Value, actorUserId.Value, request.RoleId);
        }

        private int? ResolveCompanyId(int? requestedCompanyId, out bool forbidden)
        {
            forbidden = false;
            var raw = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(raw, out var claimCompanyId))
                return null;

            if (requestedCompanyId.HasValue && requestedCompanyId.Value != claimCompanyId)
            {
                forbidden = true;
                return null;
            }

            return claimCompanyId;
        }

        private int? GetCurrentUserId()
        {
            var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(raw, out var id) ? id : null;
        }
    }
}

