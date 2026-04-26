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
    public class SupportTicketsController : ControllerBase
    {
        private readonly ISupportTicketService _service;

        public SupportTicketsController(ISupportTicketService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetByCompany([FromQuery] int companyId, [FromQuery] byte? status, [FromQuery] byte? priority, [FromQuery] bool onlyOverdue = false)
        {
            return await _service.GetByCompanyAsync(companyId, status, priority, onlyOverdue);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupportTicketRequest request, [FromQuery] int companyId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();
            return await _service.CreateAsync(request, companyId, userId.Value);
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
        public async Task<IActionResult> Analytics([FromQuery] int companyId)
        {
            return await _service.GetAnalyticsAsync(companyId);
        }

        private int? GetCurrentUserId()
        {
            var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }
    }
}
