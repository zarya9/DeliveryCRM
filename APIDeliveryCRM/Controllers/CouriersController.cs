using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CouriersController : Controller
    {
        private static readonly string[] StaffRoles = { "Менеджер", "Логист", "Администратор", "Админ" };

        private readonly ICourierService _courierService;
        private readonly IShiftService _shiftService;
        private readonly ContextDB _context;

        public CouriersController(ICourierService courierService, IShiftService shiftService, ContextDB context)
        {
            _courierService = courierService;
            _shiftService = shiftService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? companyId)
        {
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Forbid();
            if (!resolvedCompanyId.HasValue) return Unauthorized();

            var list = await _courierService.GetAllAsync(resolvedCompanyId);
            return new OkObjectResult(list);
        }

        [HttpGet("by-user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var profile = await _courierService.GetByUserIdAsync(userId);
            if (profile == null)
                return new NotFoundResult();

            var companyId = GetCompanyIdClaim();
            if (!companyId.HasValue || profile.Company_id != companyId.Value)
                return Forbid();

            return new OkObjectResult(profile);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            var profile = await _courierService.GetProfileAsync(id);
            if (profile == null)
            {
                return new NotFoundResult();
            }

            var companyId = GetCompanyIdClaim();
            if (!companyId.HasValue || profile.Company_id != companyId.Value)
                return Forbid();

            return new OkObjectResult(profile);
        }

        [HttpGet("{id:int}/orders")]
        public async Task<IActionResult> GetActiveOrders(int id)
        {
            var err = await AuthorizeCourierSelfOrStaffAsync(id);
            if (err != null)
                return err;

            var orders = await _courierService.GetActiveOrdersAsync(id);
            return new OkObjectResult(orders);
        }

        [Authorize(Roles = "Логист,Администратор,Админ,Менеджер")]
        [HttpGet("{id:int}/route-map")]
        public async Task<IActionResult> GetRouteMap(int id)
        {
            var err = await AuthorizeCourierSelfOrStaffAsync(id);
            if (err != null)
                return err;

            var profile = await _courierService.GetProfileAsync(id);
            var orders = await _courierService.GetActiveOrdersAsync(id);
            var hubs = await _context.LogisticsHubs
                .AsNoTracking()
                .Include(h => h.Address)
                .Where(h => h.Company_id == profile.Company_id)
                .ToListAsync();

            var markers = new List<object>();
            if (profile.Current_lat != 0 || profile.Current_lon != 0)
            {
                markers.Add(new
                {
                    kind = "courier",
                    id = profile.ID_CourierProfile,
                    lat = (double)profile.Current_lat,
                    lon = (double)profile.Current_lon,
                    title = $"Курьер: {profile.User?.FName} {profile.User?.Name}".Trim()
                });
            }

            foreach (var hub in hubs)
            {
                if (hub.Address?.Latitude is null || hub.Address.Longitude is null)
                    continue;
                markers.Add(new
                {
                    kind = "hub",
                    id = hub.ID_LogisticsHub,
                    lat = (double)hub.Address.Latitude.Value,
                    lon = (double)hub.Address.Longitude.Value,
                    title = $"Склад: {hub.Name}"
                });
            }

            var waypoints = new List<object>();
            foreach (var order in orders.OrderBy(o => o.Created_at))
            {
                if (order.RouteStops?.Count > 0)
                {
                    foreach (var stop in order.RouteStops.OrderBy(s => s.SortOrder))
                    {
                        decimal? lat = stop.Address?.Latitude ?? stop.LogisticsHub?.Address?.Latitude;
                        decimal? lon = stop.Address?.Longitude ?? stop.LogisticsHub?.Address?.Longitude;
                        if (!lat.HasValue || !lon.HasValue) continue;
                        waypoints.Add(new
                        {
                            orderId = order.ID_Order,
                            title = $"Заказ #{order.Order_Number}: {(string.IsNullOrWhiteSpace(stop.Title) ? stop.Kind.ToString() : stop.Title)}",
                            lat = (double)lat.Value,
                            lon = (double)lon.Value
                        });
                    }
                    continue;
                }

                if (order.PickupAddress?.Latitude is { } pLat && order.PickupAddress.Longitude is { } pLon)
                {
                    waypoints.Add(new
                    {
                        orderId = order.ID_Order,
                        title = $"Заказ #{order.Order_Number}: забор",
                        lat = (double)pLat,
                        lon = (double)pLon
                    });
                }
                if (order.DeliveryAddress?.Latitude is { } dLat && order.DeliveryAddress.Longitude is { } dLon)
                {
                    waypoints.Add(new
                    {
                        orderId = order.ID_Order,
                        title = $"Заказ #{order.Order_Number}: доставка",
                        lat = (double)dLat,
                        lon = (double)dLon
                    });
                }
            }

            return Ok(new
            {
                courierId = profile.ID_CourierProfile,
                courierName = $"{profile.User?.FName} {profile.User?.Name}".Trim(),
                markers,
                waypoints
            });
        }

        [HttpGet("vehicles")]
        public async Task<IActionResult> GetVehiclesByCompany([FromQuery] int? companyId = null)
        {
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Forbid();
            if (!resolvedCompanyId.HasValue) return Unauthorized();

            var list = await _courierService.GetVehiclesByCompanyAsync(resolvedCompanyId.Value);
            return new OkObjectResult(list);
        }

        [Authorize(Roles = "Менеджер,Администратор,Админ,Логист")]
        [HttpPut("{id:int}/documents")]
        public async Task<IActionResult> UpdateDocuments(int id, [FromBody] UpdateCourierDocumentsRequest? body)
        {
            var companyId = GetCompanyIdClaim();
            if (!companyId.HasValue)
                return Unauthorized();

            try
            {
                await _courierService.UpdateCourierDocumentsAsync(
                    id,
                    companyId.Value,
                    body?.DriverLicense,
                    body?.PassportData);
                return new OkResult();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Логист,Администратор,Админ,Менеджер")]
        [HttpPost("{id:int}/assign-vehicle")]
        public async Task<IActionResult> AssignVehicle(int id, [FromQuery] int vehicleId)
        {
            try
            {
                var userId = ParseUserId(User);
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _courierService.AssignVehicleAsync(id, vehicleId, userId, ip);
                return new OkResult();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/location")]
        public async Task<IActionResult> UpdateLocation(int id, [FromQuery] decimal lat, [FromQuery] decimal lon)
        {
            var err = await AuthorizeCourierSelfOrStaffAsync(id);
            if (err != null)
                return err;

            try
            {
                await _courierService.UpdateLocationAsync(id, lat, lon);
                return new OkResult();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/online")]
        public async Task<IActionResult> SetOnline(int id, [FromQuery] bool isOnline)
        {
            var err = await AuthorizeCourierSelfOrStaffAsync(id);
            if (err != null)
                return err;

            try
            {
                await _courierService.SetOnlineStatusAsync(id, isOnline);
                return new OkResult();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}/shift/active")]
        public async Task<IActionResult> GetActiveShift(int id)
        {
            var err = await AuthorizeCourierSelfOrStaffAsync(id);
            if (err != null)
                return err;

            var s = await _shiftService.GetActiveShiftAsync(id);
            if (s == null)
                return Ok(new { active = false });
            return Ok(new { active = true, shiftId = s.ID_Shift, timeStart = s.TimeStart });
        }

        [HttpPost("{id:int}/shift/start")]
        public async Task<IActionResult> StartShift(int id)
        {
            var err = await AuthorizeCourierSelfOrStaffAsync(id);
            if (err != null)
                return err;

            try
            {
                var shift = await _shiftService.StartShiftAsync(id);
                return new OkObjectResult(new { shiftId = shift.ID_Shift, timeStart = shift.TimeStart });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("shift/{shiftId:int}/end")]
        public async Task<IActionResult> EndShift(int shiftId)
        {
            var shift = await _shiftService.GetByIdAsync(shiftId);
            if (shift == null)
                return NotFound();

            var companyId = GetCompanyIdClaim();
            if (!companyId.HasValue || shift.Company_id != companyId.Value)
                return Forbid();

            if (!IsLogisticsStaff())
            {
                var uid = ParseUserId(User);
                if (!uid.HasValue || shift.CourierProfile.User_id != uid.Value)
                    return Forbid();
            }

            var result = await _shiftService.EndShiftAsync(shiftId);
            if (!result)
            {
                return new BadRequestResult();
            }

            return new OkResult();
        }

        private bool IsLogisticsStaff()
        {
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToHashSet();
            return roles.Any(r => StaffRoles.Contains(r));
        }

        private async Task<IActionResult?> AuthorizeCourierSelfOrStaffAsync(int courierProfileId)
        {
            CourierProfile profile;
            try
            {
                profile = await _courierService.GetProfileAsync(courierProfileId);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            var companyId = GetCompanyIdClaim();
            if (!companyId.HasValue || profile.Company_id != companyId.Value)
                return Forbid();

            if (!IsLogisticsStaff())
            {
                var uid = ParseUserId(User);
                if (!uid.HasValue || profile.User_id != uid.Value)
                    return Forbid();
            }

            return null;
        }

        private static int? ParseUserId(ClaimsPrincipal user)
        {
            var v = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }

        private int? GetCompanyIdClaim()
        {
            var raw = User.FindFirst("companyId")?.Value;
            return int.TryParse(raw, out var id) ? id : null;
        }

        private int? ResolveCompanyId(int? requestedCompanyId, out bool forbidden)
        {
            forbidden = false;
            var claimCompanyId = GetCompanyIdClaim();
            if (!claimCompanyId.HasValue)
                return null;

            if (requestedCompanyId.HasValue && requestedCompanyId.Value != claimCompanyId.Value)
            {
                forbidden = true;
                return null;
            }

            return claimCompanyId.Value;
        }
    }
}
