using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
public class SupportTicketsController : Controller
    {
        private readonly ISupportTicketService _service;

        public SupportTicketsController(ISupportTicketService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetByCompany([FromQuery] int? companyId = null, [FromQuery] byte? status = null, [FromQuery] byte? priority = null, [FromQuery] bool onlyOverdue = false)
        {
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Forbid();
            if (!resolvedCompanyId.HasValue) return Unauthorized();
            return await _service.GetByCompanyAsync(resolvedCompanyId.Value, status, priority, onlyOverdue);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupportTicketRequest request, [FromQuery] int? companyId = null)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Forbid();
            if (!resolvedCompanyId.HasValue) return Unauthorized();
            return await _service.CreateAsync(request, resolvedCompanyId.Value, userId.Value);
        }

        [HttpPost("{id:int}/assign")]
        public async Task<IActionResult> Assign(int id, [FromQuery] int responsibleUserId)
        {
            var actor = GetCurrentUserId();
            if (!actor.HasValue)
                return Unauthorized();
            return await _service.AssignAsync(id, responsibleUserId, actor.Value);
        }

        [HttpPost("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateSupportTicketStatusRequest request)
        {
            var actor = GetCurrentUserId();
            if (!actor.HasValue)
                return Unauthorized();
            return await _service.UpdateStatusAsync(id, request, actor.Value);
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> Analytics([FromQuery] int? companyId = null)
        {
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Forbid();
            if (!resolvedCompanyId.HasValue) return Unauthorized();
            return await _service.GetAnalyticsAsync(resolvedCompanyId.Value);
        }

        private int? GetCurrentUserId()
        {
            var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(v, out var id) ? id : null;
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
    }
}
