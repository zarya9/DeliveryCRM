using System;
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
    public class CouriersController : Controller
    {
        private readonly ICourierService _courierService;
        private readonly IShiftService _shiftService;

        public CouriersController(ICourierService courierService, IShiftService shiftService)
        {
            _courierService = courierService;
            _shiftService = shiftService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? companyId)
        {
            var list = await _courierService.GetAllAsync(companyId);
            return new OkObjectResult(list);
        }

        [HttpGet("by-user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var profile = await _courierService.GetByUserIdAsync(userId);
            if (profile == null)
                return new NotFoundResult();
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

            return new OkObjectResult(profile);
        }

        [HttpGet("{id:int}/orders")]
        public async Task<IActionResult> GetActiveOrders(int id)
        {
            var orders = await _courierService.GetActiveOrdersAsync(id);
            return new OkObjectResult(orders);
        }

        [HttpGet("vehicles")]
        public async Task<IActionResult> GetVehiclesByCompany([FromQuery] int companyId)
        {
            var list = await _courierService.GetVehiclesByCompanyAsync(companyId);
            return new OkObjectResult(list);
        }

        [Authorize(Roles = "Менеджер,Админ")]
        [HttpPut("{id:int}/documents")]
        public async Task<IActionResult> UpdateDocuments(int id, [FromBody] UpdateCourierDocumentsRequest? body)
        {
            try
            {
                await _courierService.UpdateCourierDocumentsAsync(
                    id,
                    body?.DriverLicense,
                    body?.PassportData);
                return new OkResult();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Логист,Админ,Менеджер")]
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
            await _courierService.UpdateLocationAsync(id, lat, lon);
            return new OkResult();
        }

        [HttpPost("{id:int}/online")]
        public async Task<IActionResult> SetOnline(int id, [FromQuery] bool isOnline)
        {
            await _courierService.SetOnlineStatusAsync(id, isOnline);
            return new OkResult();
        }

        [HttpPost("{id:int}/shift/start")]
        public async Task<IActionResult> StartShift(int id)
        {
            var shift = await _shiftService.StartShiftAsync(id);
            return new OkObjectResult(shift);
        }

        [HttpPost("shift/{shiftId:int}/end")]
        public async Task<IActionResult> EndShift(int shiftId)
        {
            var result = await _shiftService.EndShiftAsync(shiftId);
            if (!result)
            {
                return new BadRequestResult();
            }

            return new OkResult();
        }

        private static int? ParseUserId(ClaimsPrincipal user)
        {
            var v = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }
    }
}


