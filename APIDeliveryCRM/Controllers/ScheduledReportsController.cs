using System.Security.Claims;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Менеджер,Админ")]
    public class ScheduledReportsController : ControllerBase
    {
        private readonly IScheduledReportService _service;

        public ScheduledReportsController(IScheduledReportService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });
            return await _service.GetByCompanyAsync(companyId.Value);
        }

        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] UpsertScheduledReportJobRequest request)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });
            return await _service.UpsertAsync(companyId.Value, request);
        }

        [HttpPost("{id:int}/run-now")]
        public async Task<IActionResult> RunNow(int id)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });
            return await _service.RunNowAsync(id, companyId.Value);
        }

        private int? GetCompanyId()
        {
            var raw = User.FindFirst("companyId")?.Value
                      ?? User.FindFirst(ClaimTypes.GroupSid)?.Value;
            return int.TryParse(raw, out var id) ? id : null;
        }
    }
}
