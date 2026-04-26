using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeadsController : ControllerBase
    {
        private readonly ILeadService _leadService;

        public LeadsController(ILeadService leadService)
        {
            _leadService = leadService;
        }

        [HttpGet]
        public async Task<IActionResult> GetByCompany([FromQuery] int companyId)
        {
            return await _leadService.GetByCompanyAsync(companyId);
        }

        [HttpGet("meta")]
        public async Task<IActionResult> GetMeta()
        {
            return await _leadService.GetMetaAsync();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateLeadRequest request,
            [FromQuery] int companyId,
            [FromQuery] int managerUserId)
        {
            return await _leadService.CreateAsync(request, companyId, managerUserId);
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
        public async Task<IActionResult> Analytics([FromQuery] int companyId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return await _leadService.GetAnalyticsAsync(companyId, from, to);
        }
    }
}

