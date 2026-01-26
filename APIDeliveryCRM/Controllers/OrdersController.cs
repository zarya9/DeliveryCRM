using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
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
            var created = await _orderService.CreateAsync(request);
            return new OkObjectResult(created);
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
    }
}


