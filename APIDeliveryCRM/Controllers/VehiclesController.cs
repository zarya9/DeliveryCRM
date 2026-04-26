using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiclesController : ControllerBase
    {
        private readonly ICourierService _courierService;
        private readonly ContextDB _db;

        public VehiclesController(ICourierService courierService, ContextDB db)
        {
            _courierService = courierService;
            _db = db;
        }

        /// <summary>Справочники для формы создания ТС: категория, кузов, топливо. Марка/модель — вручную.</summary>
        [Authorize(Roles = "Логист,Админ,Менеджер")]
        [HttpGet("lookups")]
        public async Task<IActionResult> GetLookups()
        {
            var categories = await _db.VehicleCategories.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new IdNameDto { Id = c.ID_Category, Name = c.Name })
                .ToListAsync();

            var bodyTypes = await _db.VehicleBodyTypes.AsNoTracking()
                .OrderBy(b => b.Name)
                .Select(b => new IdNameDto { Id = b.ID_BodyType, Name = b.Name })
                .ToListAsync();

            var fuelTypes = await _db.FuelTypes.AsNoTracking()
                .OrderBy(f => f.Name)
                .Select(f => new IdNameDto { Id = f.ID_FuelType, Name = f.Name })
                .ToListAsync();

            return Ok(new VehicleFormLookupsDto
            {
                Categories = categories,
                BodyTypes = bodyTypes,
                FuelTypes = fuelTypes
            });
        }

        /// <summary>Создание ТС в автопарке компании (JWT companyId).</summary>
        [Authorize(Roles = "Логист,Админ,Менеджер")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVehicleRequest dto)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });

            var userId = GetUserId();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                var vehicle = await _courierService.CreateVehicleAsync(dto, companyId.Value, userId, ip);
                return Ok(new { id = vehicle.ID_Vehicle });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>ТС с истекающими документами в ближайшие N дней.</summary>
        [Authorize(Roles = "Логист,Админ,Менеджер")]
        [HttpGet("expiring-docs")]
        public async Task<IActionResult> GetExpiringDocs([FromQuery] int days = 14)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });

            if (days <= 0) days = 14;
            var now = DateTime.UtcNow;
            var until = now.AddDays(days);

            var items = await _db.Vehicles.AsNoTracking()
                .Where(v => v.Company_id == companyId.Value &&
                    ((v.Insurance_expires_at.HasValue && v.Insurance_expires_at.Value <= until) ||
                     (v.Registration_expires_at.HasValue && v.Registration_expires_at.Value <= until) ||
                     (v.Maintenance_due_at.HasValue && v.Maintenance_due_at.Value <= until)))
                .OrderBy(v => v.Insurance_expires_at ?? DateTime.MaxValue)
                .ThenBy(v => v.Registration_expires_at ?? DateTime.MaxValue)
                .ThenBy(v => v.Maintenance_due_at ?? DateTime.MaxValue)
                .Select(v => new
                {
                    id = v.ID_Vehicle,
                    plate = v.License_plate,
                    insuranceExpiresAt = v.Insurance_expires_at,
                    registrationExpiresAt = v.Registration_expires_at,
                    maintenanceDueAt = v.Maintenance_due_at,
                    isAvailable = v.Is_available
                })
                .ToListAsync();

            return Ok(items);
        }

        private int? GetCompanyId()
        {
            var v = User.FindFirst("companyId")?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }

        private int? GetUserId()
        {
            var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }

        public class VehicleFormLookupsDto
        {
            public System.Collections.Generic.List<IdNameDto> Categories { get; set; } = new();
            public System.Collections.Generic.List<IdNameDto> BodyTypes { get; set; } = new();
            public System.Collections.Generic.List<IdNameDto> FuelTypes { get; set; } = new();
        }

        public class IdNameDto
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }
    }
}
