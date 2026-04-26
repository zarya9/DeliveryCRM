using System;
using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? companyId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var orders = await _orderService.GetAllAsync(companyId, from, to);
            return new OkObjectResult(orders);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(order);
        }

        [HttpGet("client/{clientId:int}")]
        public async Task<IActionResult> GetByClient(int clientId)
        {
            var orders = await _orderService.GetByClientAsync(clientId);
            return new OkObjectResult(orders);
        }

        [HttpGet("courier/{courierId:int}")]
        public async Task<IActionResult> GetByCourier(int courierId)
        {
            var orders = await _orderService.GetByCourierAsync(courierId);
            return new OkObjectResult(orders);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
        {
            try
            {
                var created = await _orderService.CreateAsync(request);
                return new OkObjectResult(created);
            }
            catch (InvalidOperationException ex)
            {
                return new BadRequestObjectResult(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Order order)
        {
            if (id != order.ID_Order)
            {
                return new BadRequestResult();
            }

            var updated = await _orderService.UpdateAsync(order);
            return new OkObjectResult(updated);
        }

        [HttpPost("{id:int}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromQuery] int statusId)
        {
            var result = await _orderService.ChangeStatusAsync(id, statusId);
            if (!result)
            {
                return new NotFoundResult();
            }

            return new OkResult();
        }

        [HttpPost("{id:int}/assign")]
        public async Task<IActionResult> AssignCourier(int id, [FromQuery] int courierId)
        {
            var result = await _orderService.AssignCourierAsync(id, courierId);
            if (!result)
            {
                return new NotFoundResult();
            }

            return new OkResult();
        }

        [HttpPost("{id:int}/assign/override")]
        public async Task<IActionResult> ManualOverrideAssign(int id, [FromQuery] int courierId, [FromQuery] string? reason)
        {
            var actorUserId = GetCurrentUserId();
            var result = await _orderService.ManualOverrideCourierAsync(id, courierId, reason, actorUserId);
            if (!result)
                return NotFound();

            return Ok();
        }

        [HttpPost("{id:int}/auto-dispatch")]
        public async Task<IActionResult> AutoDispatch(int id)
        {
            var result = await _orderService.AutoDispatchAsync(id);
            if (result == null)
                return NotFound(new { message = "Заказ не найден или нет доступных онлайн-курьеров." });

            return Ok(result);
        }

        [HttpGet("{id:int}/timeline")]
        public async Task<IActionResult> Timeline(int id)
        {
            var timeline = await _orderService.GetTimelineAsync(id);
            return Ok(timeline);
        }

        [HttpGet("{id:int}/eta")]
        public async Task<IActionResult> Eta(int id)
        {
            var eta = await _orderService.GetEtaAsync(id);
            if (eta == null)
                return NotFound();
            return Ok(eta);
        }

        private int? GetCurrentUserId()
        {
            var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }
    }
}


