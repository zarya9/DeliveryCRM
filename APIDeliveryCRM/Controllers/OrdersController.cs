using System;
using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using APIDeliveryCRM.ContextDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ContextDB _context;

        public OrdersController(IOrderService orderService, ContextDB context)
        {
            _orderService = orderService;
            _context = context;
        }

        [HttpGet("statuses")]
        public async Task<IActionResult> GetOrderStatuses()
        {
            var list = await _context.OrderStatuses.AsNoTracking()
                .OrderBy(s => s.ID_OrderStatus)
                .Select(s => new { id = s.ID_OrderStatus, name = s.Name })
                .ToListAsync();
            return new OkObjectResult(list);
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
            if (!await HasActiveSubscriptionAsync(companyId.Value))
                return StatusCode(402, new { message = "РўР°СЂРёС„ РєРѕРјРїР°РЅРёРё РЅРµР°РєС‚РёРІРµРЅ. РћС„РѕСЂРјРёС‚Рµ РёР»Рё РїСЂРѕРґР»РёС‚Рµ РїРѕРґРїРёСЃРєСѓ." });
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

        [HttpPost("create-mine")]
        public async Task<IActionResult> CreateMine([FromBody] CustomerCreateOrderRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var client = await _context.ClientProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.User_id == userId.Value);
            if (client == null)
                return BadRequest(new { message = "РџСЂРѕС„РёР»СЊ РєР»РёРµРЅС‚Р° РЅРµ РЅР°Р№РґРµРЅ РґР»СЏ С‚РµРєСѓС‰РµРіРѕ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ." });
            if (!await HasActiveSubscriptionAsync(client.Company_id))
                return StatusCode(402, new { message = "РўР°СЂРёС„ РєРѕРјРїР°РЅРёРё РЅРµР°РєС‚РёРІРµРЅ. РЎРѕР·РґР°РЅРёРµ Р·Р°РєР°Р·РѕРІ РІСЂРµРјРµРЅРЅРѕ РЅРµРґРѕСЃС‚СѓРїРЅРѕ." });

            var orderTypeId = await _context.OrderTypes
                .AsNoTracking()
                .Select(x => x.ID_OrderType)
                .FirstOrDefaultAsync();
            var statusId = await _context.OrderStatuses
                .AsNoTracking()
                .Select(x => x.ID_OrderStatus)
                .FirstOrDefaultAsync();
            var packageTypeId = await _context.PackageTypes
                .AsNoTracking()
                .Select(x => x.ID_PackageType)
                .FirstOrDefaultAsync();
            var fallbackPaymentMethodId = await _context.PaymentMethods
                .AsNoTracking()
                .Select(x => x.ID_PaymentMethod)
                .FirstOrDefaultAsync();

            if (orderTypeId == 0 || statusId == 0 || packageTypeId == 0)
                return BadRequest(new { message = "РќРµ РЅР°СЃС‚СЂРѕРµРЅС‹ СЃРїСЂР°РІРѕС‡РЅРёРєРё Р·Р°РєР°Р·Р° (С‚РёРїС‹/СЃС‚Р°С‚СѓСЃС‹/РїР°РєРµС‚С‹)." });
            if (client.Preferred_payment_method_id <= 0 && fallbackPaymentMethodId == 0)
                return BadRequest(new { message = "РќРµ РЅР°СЃС‚СЂРѕРµРЅС‹ СЃРїРѕСЃРѕР±С‹ РѕРїР»Р°С‚С‹ РґР»СЏ РєРѕРјРїР°РЅРёРё." });

            var pickupAddress = new Address
            {
                Street = request.PickupStreet.Trim(),
                House = request.PickupHouse.Trim(),
                Flat = string.IsNullOrWhiteSpace(request.PickupFlat) ? null : request.PickupFlat.Trim(),
                City = string.IsNullOrWhiteSpace(request.PickupCity) ? null : request.PickupCity.Trim(),
                Comment = string.IsNullOrWhiteSpace(request.PickupComment) ? null : request.PickupComment.Trim(),
                Company_id = client.Company_id,
                User_id = userId.Value
            };
            var deliveryAddress = new Address
            {
                Street = request.DeliveryStreet.Trim(),
                House = request.DeliveryHouse.Trim(),
                Flat = string.IsNullOrWhiteSpace(request.DeliveryFlat) ? null : request.DeliveryFlat.Trim(),
                City = string.IsNullOrWhiteSpace(request.DeliveryCity) ? null : request.DeliveryCity.Trim(),
                Comment = string.IsNullOrWhiteSpace(request.DeliveryComment) ? null : request.DeliveryComment.Trim(),
                Company_id = client.Company_id,
                User_id = userId.Value
            };

            _context.Addresses.Add(pickupAddress);
            _context.Addresses.Add(deliveryAddress);
            await _context.SaveChangesAsync();

            var create = new CreateOrderRequest
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                Client_id = client.ID_ClientProfile,
                OrderType_id = orderTypeId,
                Status_id = statusId,
                PackageType_id = packageTypeId,
                Weight = request.Weight,
                Height = request.Height,
                Length = request.Length,
                Width = request.Width,
                Estimated_cost = 0,
                PaymentMethod_id = client.Preferred_payment_method_id > 0 ? client.Preferred_payment_method_id : fallbackPaymentMethodId,
                PickupAddress_id = pickupAddress.ID_Address,
                DeliveryAddress_id = deliveryAddress.ID_Address,
                DeliveryRouteKind = 1,
                Priority = request.Priority,
                RequestedDeliveryAtUtc = request.RequestedDeliveryAtUtc
            };

            try
            {
                var created = await _orderService.CreateAsync(create);
                return Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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

        [HttpPost("{id:int}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromQuery] int statusId)
        {
            var companyId = ResolveCompanyId(null, out var forbidden);
            if (forbidden) return Forbid();
            if (!companyId.HasValue) return Unauthorized();
            var order = await _orderService.GetByIdAsync(id);
            if (order == null) return NotFound();
            if (order.Company_id != companyId.Value) return Forbid();
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
                return NotFound(new { message = "Р—Р°РєР°Р· РЅРµ РЅР°Р№РґРµРЅ РёР»Рё РЅРµС‚ РґРѕСЃС‚СѓРїРЅС‹С… РѕРЅР»Р°Р№РЅ-РєСѓСЂСЊРµСЂРѕРІ." });

            return Ok(result);
        }

        [HttpGet("{id:int}/timeline")]
        public async Task<IActionResult> Timeline(int id)
        {
            var timeline = await _orderService.GetTimelineAsync(id);
            return Ok(timeline);
        }

        [HttpPost("{id:int}/pay")]
        [Authorize(Roles = "РљР»РёРµРЅС‚")]
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

        private async Task<bool> HasActiveSubscriptionAsync(int companyId)
        {
            var company = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ID_Company == companyId);
            if (company == null || !company.Is_Active)
                return false;
            return company.SubscriptionExpiresAt == default || company.SubscriptionExpiresAt >= DateTime.UtcNow;
        }
    }
}


