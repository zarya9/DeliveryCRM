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
public class LeadsController : Controller
    {
        private readonly ILeadService _leadService;

        public LeadsController(ILeadService leadService)
        {
            _leadService = leadService;
        }

        [HttpGet]
        public async Task<IActionResult> GetByCompany([FromQuery] int? companyId = null)
        {
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Forbid();
            if (!resolvedCompanyId.HasValue) return Unauthorized();
            return await _leadService.GetByCompanyAsync(resolvedCompanyId.Value);
        }

        [HttpGet("meta")]
        public async Task<IActionResult> GetMeta()
        {
            return await _leadService.GetMetaAsync();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateLeadRequest request,
            [FromQuery] int? companyId = null,
            [FromQuery] int? managerUserId = null)
        {
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Forbid();
            if (!resolvedCompanyId.HasValue) return Unauthorized();

            var currentUserId = GetCurrentUserId();
            var actorUserId = managerUserId ?? currentUserId;
            if (!actorUserId.HasValue) return Unauthorized();

            return await _leadService.CreateAsync(request, resolvedCompanyId.Value, actorUserId.Value);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] CreateLeadRequest request,
            [FromQuery] int? companyId = null,
            [FromQuery] int? managerUserId = null)
        {
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Forbid();
            if (!resolvedCompanyId.HasValue) return Unauthorized();

            var currentUserId = GetCurrentUserId();
            var actorUserId = managerUserId ?? currentUserId;
            if (!actorUserId.HasValue) return Unauthorized();

            return await _leadService.UpdateAsync(id, request, resolvedCompanyId.Value, actorUserId.Value);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] int? companyId = null)
        {
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Forbid();
            if (!resolvedCompanyId.HasValue) return Unauthorized();

            return await _leadService.DeleteAsync(id, resolvedCompanyId.Value);
        }

        [HttpPost("{id:int}/stage")]
        public async Task<IActionResult> UpdateStage(int id, [FromQuery] int stageId)
        {
            return await _leadService.UpdateStageAsync(id, stageId);
        }

        [HttpPost("{id:int}/lost")]
        public async Task<IActionResult> MarkLost(int id, [FromQuery] string reason)
        {
            return await _leadService.MarkLostAsync(id, reason);
        }

        [HttpPost("{id:int}/won")]
        public async Task<IActionResult> MarkWon(int id)
        {
            return await _leadService.MarkWonAsync(id);
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> Analytics([FromQuery] int? companyId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Forbid();
            if (!resolvedCompanyId.HasValue) return Unauthorized();
            return await _leadService.GetAnalyticsAsync(resolvedCompanyId.Value, from, to);
        }

        private int? GetCurrentUserId()
        {
            var raw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(raw, out var id) ? id : null;
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

