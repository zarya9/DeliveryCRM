using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouriersController
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
    }
}


