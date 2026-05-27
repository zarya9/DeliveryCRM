using System;
using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using APIDeliveryCRM.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICompanyService _companyService;
        private readonly ContextDB _context;

        public OrdersController(IOrderService orderService, ICompanyService companyService, ContextDB context)
        {
            _orderService = orderService;
            _companyService = companyService;
            _context = context;
        }

        [HttpGet("statuses")]
        public async Task<IActionResult> GetOrderStatuses()
        {
            var list = await _orderService.GetOrderStatusesListAsync();
            return Ok(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? companyId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Forbid();
            if (!resolvedCompanyId.HasValue) return Unauthorized();

            var orders = await _orderService.GetAllAsync(resolvedCompanyId, from, to);
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
            var companyId = ResolveCompanyId(null, out var forbidden);
            if (forbidden) return Forbid();
            if (!companyId.HasValue) return Unauthorized();
            if (order.Company_id != companyId.Value) return Forbid();

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
            var companyId = ResolveCompanyId(null, out var forbidden);
            if (forbidden) return Forbid();
            if (!companyId.HasValue) return Unauthorized();
            if (!await _companyService.HasActiveSubscriptionAsync(companyId.Value))
                return StatusCode(402, new { message = "Тариф компании неактивен. Оформите или продлите подписку." });
            try
            {
                request.OrderCompany_id = null;
                var created = await _orderService.CreateAsync(request);
                return new OkObjectResult(created);
            }
            catch (InvalidOperationException ex)
            {
                return new BadRequestObjectResult(new { message = ex.Message });
            }
        }

        [HttpPost("create-mine")]
        public async Task<IActionResult> CreateMine([FromBody] CustomerCreateOrderRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var result = await _orderService.CreateMineFromCustomerAsync(userId.Value, request);
            return MapCustomerOrderCreateResult(result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Order order)
        {
            if (id != order.ID_Order)
            {
                return new BadRequestResult();
            }
            var companyId = ResolveCompanyId(null, out var forbidden);
            if (forbidden) return Forbid();
            if (!companyId.HasValue) return Unauthorized();
            var existing = await _orderService.GetByIdAsync(id);
            if (existing == null) return NotFound();
            if (existing.Company_id != companyId.Value) return Forbid();
            order.Company_id = existing.Company_id;

            var updated = await _orderService.UpdateAsync(order);
            return new OkObjectResult(updated);
        }

        [HttpPost("assignments/{assignmentId:int}/complete")]
        [Authorize(Roles = "Курьер,Логист,Администратор,Админ,Менеджер")]
        public async Task<IActionResult> CompleteRouteStop(int assignmentId, [FromQuery] int? courierId)
        {
            var companyId = ResolveCompanyId(null, out var forbidden);
            if (forbidden) return Forbid();
            if (!companyId.HasValue) return Unauthorized();

            var profileId = courierId;
            if (!profileId.HasValue)
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized();
                profileId = await ResolveCourierProfileIdAsync(userId.Value, companyId.Value);
            }

            if (!profileId.HasValue)
                return BadRequest(new { message = "Профиль курьера не найден." });

            var (ok, result, error) = await _orderService.CompleteRouteStopAsync(assignmentId, profileId.Value, GetCurrentUserId());
            if (!ok)
                return BadRequest(new { message = error ?? "Не удалось завершить точку маршрута." });

            return Ok(result);
        }

        [HttpPost("{id:int}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromQuery] int statusId)
        {
            var companyId = ResolveCompanyId(null, out var forbidden);
            if (forbidden) return Forbid();
            if (!companyId.HasValue) return Unauthorized();
            var order = await _orderService.GetByIdAsync(id);
            if (order == null) return NotFound();
            if (order.Company_id != companyId.Value) return Forbid();
            var result = await _orderService.ChangeStatusAsync(id, statusId, GetCurrentUserId());
            if (!result)
            {
                return new NotFoundResult();
            }

            return new OkResult();
        }

        [HttpPost("{id:int}/assign")]
        public async Task<IActionResult> AssignCourier(int id, [FromQuery] int courierId)
        {
            var companyId = ResolveCompanyId(null, out var forbidden);
            if (forbidden) return Forbid();
            if (!companyId.HasValue) return Unauthorized();
            var order = await _orderService.GetByIdAsync(id);
            if (order == null) return NotFound();
            if (order.Company_id != companyId.Value) return Forbid();
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
            var companyId = ResolveCompanyId(null, out var forbidden);
            if (forbidden) return Forbid();
            if (!companyId.HasValue) return Unauthorized();
            var order = await _orderService.GetByIdAsync(id);
            if (order == null) return NotFound();
            if (order.Company_id != companyId.Value) return Forbid();
            var actorUserId = GetCurrentUserId();
            var result = await _orderService.ManualOverrideCourierAsync(id, courierId, reason, actorUserId);
            if (!result)
                return NotFound();

            return Ok();
        }

        [HttpPost("{id:int}/revoke-courier")]
        [Authorize(Roles = "Логист,Логистика,Администратор,Админ,Менеджер")]
        public async Task<IActionResult> RevokeCourier(int id, [FromQuery] string? reason)
        {
            var companyId = ResolveCompanyId(null, out var forbidden);
            if (forbidden) return Forbid();
            if (!companyId.HasValue) return Unauthorized();

            var order = await _orderService.GetByIdAsync(id);
            if (order == null) return NotFound();
            if (order.Company_id != companyId.Value) return Forbid();

            var (ok, error) = await _orderService.RevokeCourierAsync(id, GetCurrentUserId(), reason);
            if (!ok)
                return BadRequest(new { message = error ?? "Не удалось отозвать курьера." });

            return Ok();
        }

        [HttpPost("revoke-from-courier")]
        [Authorize(Roles = "Логист,Логистика,Администратор,Админ,Менеджер")]
        public async Task<IActionResult> RevokeFromCourier([FromBody] RevokeCourierOrdersRequest request)
        {
            var companyId = ResolveCompanyId(null, out var forbidden);
            if (forbidden) return Forbid();
            if (!companyId.HasValue) return Unauthorized();
            if (request.CourierId <= 0)
                return BadRequest(new { message = "Укажите курьера." });

            var result = await _orderService.RevokeCourierOrdersAsync(
                companyId.Value,
                request.CourierId,
                request.OrderIds,
                GetCurrentUserId(),
                request.Reason);

            return Ok(result);
        }

        [HttpPost("{id:int}/auto-dispatch")]
        public async Task<IActionResult> AutoDispatch(int id)
        {
            var companyId = ResolveCompanyId(null, out var forbidden);
            if (forbidden) return Forbid();
            if (!companyId.HasValue) return Unauthorized();
            var order = await _orderService.GetByIdAsync(id);
            if (order == null) return NotFound();
            if (order.Company_id != companyId.Value) return Forbid();
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

        [HttpPost("{id:int}/pay")]
        [Authorize(Roles = "Клиент")]
        public async Task<IActionResult> PayOrder(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var (ok, err) = await _orderService.ClientCompleteOrderPaymentAsync(id, userId.Value);
            if (!ok)
                return BadRequest(new { message = err });

            return Ok(new { paid = true });
        }

        [HttpDelete("{id:int}/mine")]
        [Authorize(Roles = "Клиент")]
        public async Task<IActionResult> DeleteMine(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var (ok, err) = await _orderService.DeleteMineAsync(id, userId.Value);
            if (!ok)
                return BadRequest(new { message = err ?? "Не удалось удалить заказ." });

            return Ok(new { deleted = true });
        }

        [HttpGet("{id:int}/eta")]
        public async Task<IActionResult> Eta(int id)
        {
            var eta = await _orderService.GetEtaAsync(id);
            if (eta == null)
                return NotFound();
            return Ok(eta);
        }

        private static IActionResult MapCustomerOrderCreateResult(CustomerOrderCreateResult result)
        {
            if (result.Outcome == CustomerOrderCreateOutcome.Ok && result.Order != null)
                return new OkObjectResult(result.Order);

            var message = result.Message ?? "Ошибка создания заказа.";
            return result.Outcome switch
            {
                CustomerOrderCreateOutcome.SubscriptionInactive => new ObjectResult(new { message }) { StatusCode = 402 },
                CustomerOrderCreateOutcome.ClientNotFound or CustomerOrderCreateOutcome.CompanyNotFound
                    or CustomerOrderCreateOutcome.CatalogNotConfigured
                    or CustomerOrderCreateOutcome.PaymentMethodsNotConfigured
                    or CustomerOrderCreateOutcome.InvalidOperation => new BadRequestObjectResult(new { message }),
                _ => new BadRequestObjectResult(new { message })
            };
        }

        private int? GetCurrentUserId()
        {
            var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }

        private async Task<int?> ResolveCourierProfileIdAsync(int userId, int companyId)
        {
            return await _context.CourierProfiles.AsNoTracking()
                .Where(c => c.User_id == userId && c.Company_id == companyId)
                .Select(c => (int?)c.ID_CourierProfile)
                .FirstOrDefaultAsync();
        }

        private int? ResolveCompanyId(int? requestedCompanyId, out bool forbidden)
        {
            forbidden = false;
            var raw = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(raw, out var claimCompanyId))
                return null;

            if (requestedCompanyId.HasValue && requestedCompanyId.Value != claimCompanyId)
            {
                forbidden = true;
                return null;
            }

            return claimCompanyId;
        }
    }
}
