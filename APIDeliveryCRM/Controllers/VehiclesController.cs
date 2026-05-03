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
public class VehiclesController : Controller
    {
        private readonly ICourierService _courierService;
        private readonly ContextDB _db;

        public VehiclesController(ICourierService courierService, ContextDB db)
        {
            _courierService = courierService;
            _db = db;
        }

        /// <summary>Марки из справочника VehicleBrands.</summary>
        [Authorize(Roles = "Логист,Администратор,Админ,Менеджер")]
        [HttpGet("catalog/brands")]
        public async Task<IActionResult> GetCatalogBrands()
        {
            var list = await _db.VehicleBrands.AsNoTracking()
                .OrderBy(b => b.Name)
                .Select(b => new IdNameDto { Id = b.ID_Brand, Name = b.Name })
                .ToListAsync();
            return Ok(list);
        }

        /// <summary>Модели из справочника VehicleModels. Без brandId или brandId ≤ 0 — все модели (с маркой); иначе — только выбранная марка.</summary>
        [Authorize(Roles = "Логист,Администратор,Админ,Менеджер")]
        [HttpGet("catalog/models")]
        public async Task<IActionResult> GetCatalogModels([FromQuery] int? brandId)
        {
            var query = _db.VehicleModels.AsNoTracking().AsQueryable();
            if (brandId is > 0)
                query = query.Where(m => m.Brand_id == brandId.Value);

            var list = await query
                .OrderBy(m => m.VehicleBrand.Name)
                .ThenBy(m => m.Name)
                .ThenBy(m => m.Year)
                .Select(m => new VehicleCatalogModelDto
                {
                    Id = m.ID_Model,
                    BrandId = m.Brand_id,
                    BrandName = m.VehicleBrand.Name,
                    Name = m.Name,
                    Year = m.Year,
                    AvgFuelCity = m.AvgFuelCity,
                    AvgFuelHighWay = m.AvgFuelHighWay,
                    EngineCapacity = m.EngineCapacity,
                    HorsePower = m.HorsePower,
                    TransmissionTypeId = m.TransmissionType_id,
                    DriveTypeId = m.DriveType_id,
                    TransmissionTypeName = m.TransmissionType.Name,
                    DriveTypeName = m.VehicleDriveType.Name
                })
                .ToListAsync();
            return Ok(list);
        }

        /// <summary>Справочники для формы создания ТС: категория, кузов, топливо. Марка/модель — вручную.</summary>
        [Authorize(Roles = "Логист,Администратор,Админ,Менеджер")]
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
        [Authorize(Roles = "Логист,Администратор,Админ,Менеджер")]
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
        [Authorize(Roles = "Логист,Администратор,Админ,Менеджер")]
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

        public class VehicleCatalogModelDto
        {
            public int Id { get; set; }
            public int BrandId { get; set; }
            public string? BrandName { get; set; }
            public string? Name { get; set; }
            public DateOnly Year { get; set; }
            public decimal AvgFuelCity { get; set; }
            public decimal AvgFuelHighWay { get; set; }
            public decimal EngineCapacity { get; set; }
            public int HorsePower { get; set; }
            public int TransmissionTypeId { get; set; }
            public int DriveTypeId { get; set; }
            public string? TransmissionTypeName { get; set; }
            public string? DriveTypeName { get; set; }
        }
    }
}
