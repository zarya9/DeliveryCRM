using System.Security.Claims;
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
        public async Task<IActionResult> Create([FromBody] CreateLeadRequest request)
        {
            var companyIdStr = User.FindFirst("companyId")?.Value;
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(companyIdStr, out var companyId) ||
                !int.TryParse(userIdStr, out var managerUserId))
            {
                return BadRequest(new { message = "Не удалось определить компанию или пользователя" });
            }

            return await _leadService.CreateAsync(request, companyId, managerUserId);
        }

        [HttpPost("{id:int}/stage")]
        public async Task<IActionResult> UpdateStage(int id, [FromQuery] int stageId)
        {
            return await _leadService.UpdateStageAsync(id, stageId);
        }
    }
}

