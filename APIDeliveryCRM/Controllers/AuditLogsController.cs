using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Админ")]
    public class AuditLogsController : ControllerBase
    {
        private readonly ContextDB _db;

        public AuditLogsController(ContextDB db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int take = 500)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });

            take = take <= 0 ? 500 : System.Math.Min(take, 2000);

            var rows = await _db.AuditLogs.AsNoTracking()
                .Where(a => a.Company_id == companyId.Value)
                .OrderByDescending(a => a.Created_at)
                .Take(take)
                .Select(a => new AuditLogRowDto
                {
                    ID_AuditLog = a.ID_AuditLog,
                    TableName = a.TableName,
                    RecordId = a.RecordId,
                    Action = a.Action,
                    FieldName = a.FieldName,
                    OldValue = a.OldValue,
                    NewValue = a.NewValue,
                    Description = a.Description,
                    UserName = a.User != null ? a.User.FName + " " + a.User.Name : null,
                    User_id = a.User_id,
                    Created_at = a.Created_at
                })
                .ToListAsync();

            return Ok(rows);
        }

        private int? GetCompanyId()
        {
            var v = User.FindFirst("companyId")?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }

        public class AuditLogRowDto
        {
            public int ID_AuditLog { get; set; }
            public string TableName { get; set; } = string.Empty;
            public int RecordId { get; set; }
            public string Action { get; set; } = string.Empty;
            public string? FieldName { get; set; }
            public string? OldValue { get; set; }
            public string? NewValue { get; set; }
            public string? Description { get; set; }
            public string? UserName { get; set; }
            public int? User_id { get; set; }
            public System.DateTime Created_at { get; set; }
        }
    }
}
