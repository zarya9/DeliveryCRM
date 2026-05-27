using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Логист,Администратор,Админ,Менеджер")]
    public class ServiceAreaZonesController : Controller
    {
        private readonly IServiceAreaZoneService _service;

        public ServiceAreaZonesController(IServiceAreaZoneService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });

            var items = await _service.GetByCompanyAsync(companyId.Value);
            return Ok(items.Select(z => new
            {
                id = z.ID_ServiceAreaZone,
                name = z.Name,
                centerLat = z.Center_lat,
                centerLon = z.Center_lon,
                radiusKm = z.Radius_km,
                isActive = z.Is_active,
                courierIds = z.Couriers.Select(c => c.CourierProfile_id).ToList()
            }));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateServiceAreaZoneRequest request)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });

            var zone = await _service.CreateAsync(companyId.Value, request);
            return Ok(new { id = zone.ID_ServiceAreaZone });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateServiceAreaZoneRequest request)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });

            var zone = await _service.UpdateAsync(id, companyId.Value, request);
            if (zone == null)
                return NotFound(new { message = "Зона не найдена." });

            return Ok(new
            {
                id = zone.ID_ServiceAreaZone,
                name = zone.Name,
                centerLat = zone.Center_lat,
                centerLon = zone.Center_lon,
                radiusKm = zone.Radius_km,
                isActive = zone.Is_active,
                courierIds = zone.Couriers.Select(c => c.CourierProfile_id).ToList()
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });

            var ok = await _service.DeleteAsync(id, companyId.Value);
            if (!ok)
                return NotFound(new { message = "Зона не найдена." });

            return NoContent();
        }

        [HttpPost("{id:int}/assign-courier")]
        public async Task<IActionResult> AssignCourier(int id, [FromQuery] int courierId)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });

            var ok = await _service.AssignCourierAsync(id, courierId, companyId.Value);
            if (!ok)
                return BadRequest(new { message = "Не удалось назначить курьера в зону." });
            return Ok();
        }

        [HttpDelete("{id:int}/unassign-courier")]
        public async Task<IActionResult> UnassignCourier(int id, [FromQuery] int courierId)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });

            var ok = await _service.UnassignCourierAsync(id, courierId, companyId.Value);
            if (!ok)
                return NotFound(new { message = "Связь курьер-зона не найдена." });
            return NoContent();
        }

        private int? GetCompanyId()
        {
            var v = User.FindFirst("companyId")?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }
    }
}
