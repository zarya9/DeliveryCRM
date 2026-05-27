using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Hubs;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using APIDeliveryCRM.Responses;
using APIDeliveryCRM.Utilities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class OrderService : IOrderService
    {
        private readonly ContextDB _context;
        private readonly ICommunicationTemplateService _templateService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly IFuelPriceService _fuelPriceService;
        private readonly IConfiguration _configuration;
        private readonly IShiftPlannerService _shiftPlanner;
        private readonly ICompanyService _companyService;

        public OrderService(
            ContextDB context,
            ICommunicationTemplateService templateService,
            INotificationService notificationService,
            IHubContext<ChatHub> hubContext,
            IKafkaProducer kafkaProducer,
            IFuelPriceService fuelPriceService,
            IConfiguration configuration,
            IShiftPlannerService shiftPlanner,
            ICompanyService companyService)
        {
            _context = context;
            _templateService = templateService;
            _notificationService = notificationService;
            _hubContext = hubContext;
            _kafkaProducer = kafkaProducer;
            _fuelPriceService = fuelPriceService;
            _configuration = configuration;
            _shiftPlanner = shiftPlanner;
            _companyService = companyService;
        }

        public async Task<Order> GetByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.ClientProfile)
                .Include(o => o.CourierProfile)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderType)
                .Include(o => o.PackageType)
                .Include(o => o.PaymentMethod)
                .Include(o => o.PickupAddress)
                .Include(o => o.DeliveryAddress)
                .Include(o => o.OriginHub)!.ThenInclude(h => h!.Address)
                .Include(o => o.DestinationHub)!.ThenInclude(h => h!.Address)
                .Include(o => o.RouteStops).ThenInclude(s => s.Address)
                .Include(o => o.RouteStops).ThenInclude(s => s.LogisticsHub)
                .FirstOrDefaultAsync(o => o.ID_Order == id);
            if (order != null)
                await EnrichClientOrdersAsync(new List<Order> { order });
            return order;
        }

        public async Task<IReadOnlyList<Order>> GetAllAsync(int? companyId = null, DateTime? fromUtc = null, DateTime? toUtc = null)
        {
            var query = _context.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.ClientProfile).ThenInclude(c => c.User)
                .Include(o => o.CourierProfile).ThenInclude(c => c!.User)
                .Include(o => o.OrderType)
                .Include(o => o.RouteStops)
                .Include(o => o.PickupAddress)
                .Include(o => o.DeliveryAddress)
                .AsQueryable();
            if (companyId.HasValue)
                query = query.Where(o => o.Company_id == companyId.Value);
            if (fromUtc.HasValue)
            {
                var from = fromUtc.Value.ToUniversalTime();
                query = query.Where(o => o.Created_at >= from);
            }
            if (toUtc.HasValue)
            {
                var to = toUtc.Value.ToUniversalTime();
                query = query.Where(o => o.Created_at <= to);
            }
            return await query.OrderByDescending(o => o.Created_at).Take(500).ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetByClientAsync(int clientProfileId)
        {
            var orders = await _context.Orders
                .Where(o => o.Client_id == clientProfileId)
                .Include(o => o.OrderStatus)
                .Include(o => o.CourierProfile)
                    .ThenInclude(c => c.User)
                .Include(o => o.OrderType)
                .Include(o => o.PackageType)
                .Include(o => o.PaymentMethod)
                .Include(o => o.PickupAddress)
                .Include(o => o.DeliveryAddress)
                .OrderByDescending(o => o.Created_at)
                .ToListAsync();
            await EnrichClientOrdersAsync(orders);
            return orders;
        }

        private async Task EnrichClientOrdersAsync(List<Order> orders)
        {
            if (orders.Count == 0)
                return;

            var orderIds = orders.Select(o => o.ID_Order).ToList();
            var refundedIds = await _context.OrderTimelineEvents.AsNoTracking()
                .Where(e => orderIds.Contains(e.Order_id) && e.EventType == "PAYMENT_REFUNDED")
                .Select(e => e.Order_id)
                .Distinct()
                .ToListAsync();
            var refundedSet = refundedIds.ToHashSet();

            var repaired = false;
            foreach (var order in orders)
            {
                order.CanDeleteByClient = CanClientDeleteOrder(order, order.OrderStatus?.Name);

                if (OrderStatusRules.IsCancelled(order.OrderStatus?.Name) &&
                    order.Is_paid &&
                    !refundedSet.Contains(order.ID_Order))
                {
                    order.Is_paid = false;
                    _context.OrderTimelineEvents.Add(new OrderTimelineEvent
                    {
                        Order_id = order.ID_Order,
                        EventType = "PAYMENT_REFUNDED",
                        Title = "Возврат оплаты",
                        Message = "Оплата отменена, средства возвращены клиенту (тестовый режим MockPay).",
                        NewStatus_id = order.Status_id
                    });
                    refundedSet.Add(order.ID_Order);
                    repaired = true;
                }

                order.WasPaymentRefunded = refundedSet.Contains(order.ID_Order);

                if (order.Delivered_at.HasValue &&
                    TryGetDeliveryWindowMismatch(order, out var mismatchKind, out _))
                {
                    order.DeliveryWindowMismatch = true;
                    order.DeliveryWindowMismatchKind = mismatchKind;
                }
            }

            if (repaired)
                await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<Order>> GetByCourierAsync(int courierProfileId)
        {
            return await _context.Orders
                .Where(o => o.Courier_id == courierProfileId)
                .Include(o => o.OrderStatus)
                .Include(o => o.ClientProfile)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<OrderStatusListItemDto>> GetOrderStatusesListAsync()
        {
            return await _context.OrderStatuses.AsNoTracking()
                .OrderBy(s => s.ID_OrderStatus)
                .Select(s => new OrderStatusListItemDto { Id = s.ID_OrderStatus, Name = s.Name })
                .ToListAsync();
        }

        public async Task<CustomerOrderCreateResult> CreateMineFromCustomerAsync(int userId, CustomerCreateOrderRequest request)
        {
            var client = await _context.ClientProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.User_id == userId);
            if (client == null)
                return CustomerOrderCreateResult.Fail(CustomerOrderCreateOutcome.ClientNotFound, "Профиль клиента не найден для текущего пользователя.");

            if (request.CompanyId == 1)
                return CustomerOrderCreateResult.Fail(CustomerOrderCreateOutcome.CompanyNotFound, "Эта компания недоступна для выбора.");

            var targetCompanyId = request.CompanyId > 0 ? request.CompanyId : client.Company_id;
            var targetCompany = await _context.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ID_Company == targetCompanyId);
            if (targetCompany == null)
                return CustomerOrderCreateResult.Fail(CustomerOrderCreateOutcome.CompanyNotFound, "Указанная компания не найдена.");

            if (!await _companyService.HasActiveSubscriptionAsync(targetCompanyId))
                return CustomerOrderCreateResult.Fail(CustomerOrderCreateOutcome.SubscriptionInactive, "Тариф выбранной компании неактивен. Создание заказов временно недоступно.");

            var orderTypeId = await _context.OrderTypes.AsNoTracking()
                .Select(x => x.ID_OrderType)
                .FirstOrDefaultAsync();
            var statusId = await _context.OrderStatuses.AsNoTracking()
                .Select(x => x.ID_OrderStatus)
                .FirstOrDefaultAsync();
            var packageTypeId = await _context.PackageTypes.AsNoTracking()
                .Select(x => x.ID_PackageType)
                .FirstOrDefaultAsync();
            var fallbackPaymentMethodId = await _context.PaymentMethods.AsNoTracking()
                .Select(x => x.ID_PaymentMethod)
                .FirstOrDefaultAsync();

            if (orderTypeId == 0 || statusId == 0 || packageTypeId == 0)
                return CustomerOrderCreateResult.Fail(CustomerOrderCreateOutcome.CatalogNotConfigured, "Не настроены справочники заказа (типы/статусы/пакеты).");
            if (client.Preferred_payment_method_id <= 0 && fallbackPaymentMethodId == 0)
                return CustomerOrderCreateResult.Fail(CustomerOrderCreateOutcome.PaymentMethodsNotConfigured, "Не настроены способы оплаты для компании.");

            var pickupAddress = new Address
            {
                Street = request.PickupStreet.Trim(),
                House = request.PickupHouse.Trim(),
                Flat = string.IsNullOrWhiteSpace(request.PickupFlat) ? null : request.PickupFlat.Trim(),
                City = string.IsNullOrWhiteSpace(request.PickupCity) ? null : request.PickupCity.Trim(),
                Comment = string.IsNullOrWhiteSpace(request.PickupComment) ? null : request.PickupComment.Trim(),
                Latitude = request.PickupLatitude,
                Longitude = request.PickupLongitude,
                Company_id = targetCompanyId,
                User_id = userId
            };
            var deliveryAddress = new Address
            {
                Street = request.DeliveryStreet.Trim(),
                House = request.DeliveryHouse.Trim(),
                Flat = string.IsNullOrWhiteSpace(request.DeliveryFlat) ? null : request.DeliveryFlat.Trim(),
                City = string.IsNullOrWhiteSpace(request.DeliveryCity) ? null : request.DeliveryCity.Trim(),
                Comment = string.IsNullOrWhiteSpace(request.DeliveryComment) ? null : request.DeliveryComment.Trim(),
                Latitude = request.DeliveryLatitude,
                Longitude = request.DeliveryLongitude,
                Company_id = targetCompanyId,
                User_id = userId
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
                AutoSelectRouteKind = true,
                Priority = request.Priority,
                RequestedDeliveryAtUtc = request.RequestedDeliveryAtUtc,
                OrderCompany_id = targetCompanyId
            };

            try
            {
                var created = await CreateAsync(create);
                return CustomerOrderCreateResult.Success(created);
            }
            catch (InvalidOperationException ex)
            {
                return CustomerOrderCreateResult.Fail(CustomerOrderCreateOutcome.InvalidOperation, ex.Message);
            }
        }

        public async Task<Order> CreateAsync(CreateOrderRequest request)
        {
            var client = await _context.ClientProfiles.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ID_ClientProfile == request.Client_id);
            if (client == null)
                throw new InvalidOperationException("Клиент не найден.");

            var orderCompanyId = request.OrderCompany_id ?? client.Company_id;

            var company = await _context.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ID_Company == orderCompanyId);
            if (company == null)
                throw new InvalidOperationException("Компания для заказа не найдена.");

            if (!company.Is_Active)
                throw new InvalidOperationException("Компания деактивирована.");

            if (company.SubscriptionExpiresAt != default && company.SubscriptionExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Подписка компании истекла. Продлите тариф для создания заказов.");

            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthlyOrders = await _context.Orders
                .AsNoTracking()
                .CountAsync(o => o.Company_id == orderCompanyId && o.Created_at >= monthStart);
            if (monthlyOrders >= company.MaxOrdersPerMonth)
                throw new InvalidOperationException("Достигнут лимит заказов по тарифу за текущий месяц.");

            var pickup = await _context.Addresses.FirstOrDefaultAsync(a =>
                a.ID_Address == request.PickupAddress_id && a.Company_id == orderCompanyId);
            var delivery = await _context.Addresses.FirstOrDefaultAsync(a =>
                a.ID_Address == request.DeliveryAddress_id && a.Company_id == orderCompanyId);
            if (pickup == null || delivery == null)
                throw new InvalidOperationException("Адреса забора или доставки не найдены или не принадлежат компании заказа.");

            var routeChoice = await ResolveRouteChoiceAsync(request, orderCompanyId, pickup, delivery);
            var routeKind = routeChoice.RouteKind;
            var originHub = routeChoice.OriginHub;
            var destHub = routeChoice.DestinationHub;
            var stops = OrderRoutePlanner.BuildStops(routeKind, pickup, delivery, originHub, destHub);
            var distanceKm = EstimateRouteDistanceKm(routeKind, pickup, delivery, originHub, destHub);
            var fuelCostRub = await EstimateFuelCostRubAsync(
                companyId: orderCompanyId,
                courierProfileId: request.Courier_id,
                routeKind: routeKind,
                distanceKm: distanceKm);
            var estimatedCost = CalculateEstimatedCost(
                distanceKm: distanceKm,
                weightKg: request.Weight,
                lengthCm: request.Length,
                widthCm: request.Width,
                heightCm: request.Height,
                priority: request.Priority,
                routeKind: routeKind,
                createdAtUtc: DateTime.UtcNow,
                fuelCostRub: fuelCostRub);
            var deliveryWindow = DeliveryWindowPolicy.Compute(DateTime.UtcNow, request.Priority);

            // EF Core не переводит Select(...).DefaultIfEmpty(0).MaxAsync() в SQL (Npgsql).
            var maxOrderNumber = await _context.Orders.MaxAsync(o => (int?)o.Order_Number) ?? 0;

            var order = new Order
            {
                Name = request.Name,
                Description = request.Description,
                Order_Number = maxOrderNumber + 1,
                Company_id = orderCompanyId,
                Client_id = request.Client_id,
                OrderType_id = request.OrderType_id,
                Status_id = request.Status_id,
                Courier_id = request.Courier_id,
                PackageType_id = request.PackageType_id,
                Weight = request.Weight,
                Height = request.Height,
                Length = request.Length,
                Width = request.Width,
                Estimated_cost = estimatedCost,
                Final_cost = 0,
                Created_at = DateTime.UtcNow,
                PaymentMethod_id = request.PaymentMethod_id,
                Is_paid = false,
                PickupAddress_id = request.PickupAddress_id,
                DeliveryAddress_id = request.DeliveryAddress_id,
                DeliveryRouteKind = routeKind,
                OriginHub_id = routeKind == DeliveryRouteKind.ViaHub ? originHub!.ID_LogisticsHub : null,
                DestinationHub_id = routeKind == DeliveryRouteKind.ViaHub ? destHub!.ID_LogisticsHub : null,
                Priority = request.Priority,
                Sla_due_at = request.RequestedDeliveryAtUtc?.ToUniversalTime() ?? deliveryWindow.SlaDueUtc,
                Eta_at = deliveryWindow.EtaUtc
            };

            foreach (var stop in stops)
                order.RouteStops.Add(stop);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _context.OrderTimelineEvents.Add(new OrderTimelineEvent
            {
                Order_id = order.ID_Order,
                EventType = "ORDER_CREATED",
                Title = "Заказ создан",
                Message = $"Создан новый заказ. Маршрут: {BuildRouteLabel(routeKind, originHub, destHub)}. Ориентировочная доставка: {deliveryWindow.DisplayText}. Предварительная стоимость: {estimatedCost:0.##} ₽."
            });
            await _context.SaveChangesAsync();
            await PublishOrderEventAsync("order.created", order, new
            {
                routeKind = order.DeliveryRouteKind.ToString(),
                priority = order.Priority,
                etaAt = order.Eta_at,
                deliveryWindowFromUtc = deliveryWindow.EtaUtc,
                deliveryWindowToUtc = deliveryWindow.SlaDueUtc,
                estimatedCost
            });
            await TryRebuildPlannerAsync(order.Company_id, "order.created");
            return order;
        }

        public async Task<Order> UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<bool> ChangeStatusAsync(int orderId, int statusId, int? actorUserId = null)
        {
            var order = await _context.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.OriginHub)
                .Include(o => o.DestinationHub)
                .FirstOrDefaultAsync(o => o.ID_Order == orderId);
            if (order == null)
            {
                return false;
            }

            var oldStatusId = order.Status_id;
            var oldStatusName = order.OrderStatus?.Name;
            var wasDeliveredBefore = order.Delivered_at.HasValue;
            var status = await _context.OrderStatuses.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ID_OrderStatus == statusId);
            order.Status_id = statusId;
            ApplyMilestoneTimestamps(order, status?.Name, statusId);
            var justDelivered = !wasDeliveredBefore && order.Delivered_at.HasValue;
            string? deliveryWindowMismatchMessage = null;
            if (justDelivered)
                deliveryWindowMismatchMessage = ApplyDeliveryWindowComplianceFields(order);
            UpdateSlaFlags(order);
            order.Eta_at = EstimateEtaUtc(DateTime.UtcNow, order.Priority, order.Courier_id.HasValue);
            var isSlaRisk = IsSlaRisk(order);

            var newStatusName = string.IsNullOrWhiteSpace(status?.Name)
                ? statusId.ToString()
                : status!.Name.Trim();
            _context.OrderTimelineEvents.Add(new OrderTimelineEvent
            {
                Order_id = order.ID_Order,
                EventType = "STATUS_CHANGED",
                Title = "Изменение статуса",
                Message = $"Статус заказа изменен: {newStatusName}",
                OldStatus_id = oldStatusId,
                NewStatus_id = statusId
            });

            var notifyStaffCancellation = false;
            var cancelWasPaid = false;
            var cancelReason = string.Empty;

            if (OrderStatusRules.IsCancelled(newStatusName) && !OrderStatusRules.IsCancelled(oldStatusName))
            {
                order.Courier_id = null;
                order.Plan_locked_shiftPlan_id = null;
                order.Plan_locked_at = null;
                (cancelWasPaid, cancelReason) = await ApplyOrderCancellationEffectsAsync(order, actorUserId, "Заказ отменён.", oldStatusId);
                notifyStaffCancellation = true;
            }

            var activeAssignment = await _context.ShiftAssignments
                .Where(a => a.Order_id == order.ID_Order && (a.Status == ShiftAssignmentStatus.Pending || a.Status == ShiftAssignmentStatus.InProgress))
                .OrderByDescending(a => a.Assignment_sequence)
                .FirstOrDefaultAsync();
            ApplyHandoffByStatusEvent(order, status?.Name, statusId, activeAssignment);

            await SendStatusAutomationAsync(order, statusId);

            if (isSlaRisk)
            {
                _context.OrderTimelineEvents.Add(new OrderTimelineEvent
                {
                    Order_id = order.ID_Order,
                    EventType = "SLA_RISK",
                    Title = "Риск SLA",
                    Message = "ETA превышает SLA дедлайн после смены статуса."
                });
                await CreateSlaRiskAlertsAsync(order);
            }

            await _context.SaveChangesAsync();
            if (notifyStaffCancellation)
                await NotifyStaffOrderCancelledAsync(order, cancelWasPaid, cancelReason, actorUserId);
            if (!string.IsNullOrWhiteSpace(deliveryWindowMismatchMessage))
                await PublishDeliveryWindowMismatchAlertsAsync(order, deliveryWindowMismatchMessage!, actorUserId);
            await PublishOrderEventAsync("order.status_changed", order, new
            {
                oldStatusId,
                newStatusId = statusId,
                isSlaRisk
            });
            await TryRebuildPlannerAsync(order.Company_id, "order.status_changed");
            return true;
        }

        public async Task<bool> AssignCourierAsync(int orderId, int courierProfileId)
        {
            var order = await _context.Orders
                .Include(o => o.PickupAddress)
                .FirstOrDefaultAsync(o => o.ID_Order == orderId);
            if (order == null)
            {
                return false;
            }

            var courier = await _context.CourierProfiles.AsNoTracking()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId && c.Company_id == order.Company_id);
            if (courier == null)
                return false;

            var allowed = await IsCourierAllowedByZonesAsync(order, courierProfileId);
            if (!allowed)
                return false;

            var courierName = FormatCourierDisplayName(courier.User);
            var oldCourierId = order.Courier_id;
            order.Courier_id = courierProfileId;
            order.Eta_at = EstimateEtaUtc(DateTime.UtcNow, order.Priority, hasCourierAssigned: true);

            _context.OrderTimelineEvents.Add(new OrderTimelineEvent
            {
                Order_id = order.ID_Order,
                EventType = "COURIER_ASSIGNED",
                Title = "Назначен курьер",
                Message = $"{courierName} назначен на заказ.",
                OldCourier_id = oldCourierId,
                NewCourier_id = courierProfileId
            });

            await _context.SaveChangesAsync();
            await PublishOrderEventAsync("order.courier_assigned", order, new
            {
                oldCourierId,
                newCourierId = courierProfileId,
                assignmentType = "standard"
            });
            await TryEnsureCourierRouteAsync(courierProfileId);
            return true;
        }

        public async Task<bool> ManualOverrideCourierAsync(int orderId, int courierProfileId, string? reason, int? actorUserId = null)
        {
            var order = await _context.Orders
                .Include(o => o.PickupAddress)
                .FirstOrDefaultAsync(o => o.ID_Order == orderId);
            if (order == null)
                return false;

            var courier = await _context.CourierProfiles.AsNoTracking()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId && c.Company_id == order.Company_id);
            if (courier == null)
                return false;

            var allowed = await IsCourierAllowedByZonesAsync(order, courierProfileId);
            if (!allowed)
                return false;

            var courierName = FormatCourierDisplayName(courier.User);
            var oldCourierId = order.Courier_id;
            order.Courier_id = courierProfileId;
            order.Eta_at = EstimateEtaUtc(DateTime.UtcNow, order.Priority, hasCourierAssigned: true);

            _context.OrderTimelineEvents.Add(new OrderTimelineEvent
            {
                Order_id = order.ID_Order,
                EventType = "MANUAL_OVERRIDE",
                Title = "Ручное переназначение",
                Message = string.IsNullOrWhiteSpace(reason)
                    ? $"{courierName} назначен на заказ вручную."
                    : reason,
                OldCourier_id = oldCourierId,
                NewCourier_id = courierProfileId,
                ActorUser_id = actorUserId
            });

            await _context.SaveChangesAsync();
            await PublishOrderEventAsync("order.courier_assigned", order, new
            {
                oldCourierId,
                newCourierId = courierProfileId,
                assignmentType = "manual_override",
                reason
            });
            await TryEnsureCourierRouteAsync(courierProfileId);
            return true;
        }

        public async Task<(bool ok, string? error)> RevokeCourierAsync(int orderId, int? actorUserId = null, string? reason = null)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.ID_Order == orderId);
            if (order == null)
                return (false, "Заказ не найден.");

            if (!order.Courier_id.HasValue)
                return (false, "Заказ не назначен курьеру.");

            var statusName = await _context.OrderStatuses.AsNoTracking()
                .Where(s => s.ID_OrderStatus == order.Status_id)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();
            if (!CanRevokeCourierFromOrder(order, statusName))
                return (false, "Нельзя отозвать доставленный или отменённый заказ.");

            var oldCourierId = order.Courier_id.Value;

            var assignments = await _context.ShiftAssignments
                .Where(a => a.Order_id == orderId &&
                            (a.Status == ShiftAssignmentStatus.Pending || a.Status == ShiftAssignmentStatus.InProgress))
                .ToListAsync();
            foreach (var assignment in assignments)
                assignment.Status = ShiftAssignmentStatus.Reassigned;

            order.Courier_id = null;
            order.Plan_locked_shiftPlan_id = null;
            order.Plan_locked_at = null;
            order.Eta_at = EstimateEtaUtc(DateTime.UtcNow, order.Priority, hasCourierAssigned: false);

            var revokedCourierName = await GetCourierDisplayNameAsync(oldCourierId);
            var message = string.IsNullOrWhiteSpace(reason)
                ? $"{revokedCourierName} снят с заказа логистом."
                : reason.Trim();

            _context.OrderTimelineEvents.Add(new OrderTimelineEvent
            {
                Order_id = order.ID_Order,
                EventType = "COURIER_REVOKED",
                Title = "Курьер отозван",
                Message = message,
                OldCourier_id = oldCourierId,
                NewCourier_id = null,
                ActorUser_id = actorUserId
            });

            await _context.SaveChangesAsync();
            await PublishOrderEventAsync("order.courier_revoked", order, new
            {
                oldCourierId,
                reason
            });

            return (true, null);
        }

        public async Task<RevokeCourierOrdersResultDto> RevokeCourierOrdersAsync(
            int companyId,
            int courierProfileId,
            IReadOnlyList<int>? orderIds,
            int? actorUserId = null,
            string? reason = null)
        {
            var courierExists = await _context.CourierProfiles.AsNoTracking()
                .AnyAsync(c => c.ID_CourierProfile == courierProfileId && c.Company_id == companyId);
            if (!courierExists)
            {
                return new RevokeCourierOrdersResultDto
                {
                    FailedCount = 1,
                    Errors = { "Курьер не найден в компании." }
                };
            }

            var query = _context.Orders
                .Where(o => o.Company_id == companyId && o.Courier_id == courierProfileId);
            if (orderIds is { Count: > 0 })
                query = query.Where(o => orderIds.Contains(o.ID_Order));

            var orders = await query.ToListAsync();
            var result = new RevokeCourierOrdersResultDto();
            foreach (var order in orders)
            {
                var (ok, error) = await RevokeCourierAsync(order.ID_Order, actorUserId, reason);
                if (ok)
                    result.RevokedCount++;
                else
                {
                    result.FailedCount++;
                    if (!string.IsNullOrWhiteSpace(error))
                        result.Errors.Add($"№{order.Order_Number}: {error}");
                }
            }

            return result;
        }

        public async Task<(bool ok, RouteStopCompletionResultDto? result, string? error)> CompleteRouteStopAsync(
            int assignmentId,
            int courierProfileId,
            int? actorUserId = null)
        {
            var assignment = await _context.ShiftAssignments
                .Include(a => a.Order).ThenInclude(o => o.RouteStops)
                .Include(a => a.Order).ThenInclude(o => o.OriginHub)
                .Include(a => a.Order).ThenInclude(o => o.DestinationHub)
                .Include(a => a.OrderRouteStop)
                .Include(a => a.ShiftPlan)
                .FirstOrDefaultAsync(a => a.ID_ShiftAssignment == assignmentId);

            if (assignment == null)
                return (false, null, "Задание маршрута не найдено.");

            var ownsAssignment = assignment.ShiftPlan?.Courier_id == courierProfileId;
            if (!ownsAssignment)
            {
                ownsAssignment = await _context.CourierShifts.AsNoTracking()
                    .AnyAsync(s => s.ID_Shift == assignment.Shift_id
                                   && s.Courier_id == courierProfileId
                                   && s.TimeEnd == null);
            }

            if (!ownsAssignment)
                return (false, null, "Нет доступа к этому заданию.");

            if (assignment.Status is ShiftAssignmentStatus.Done or ShiftAssignmentStatus.Skipped or ShiftAssignmentStatus.Reassigned)
                return (false, null, "Точка маршрута уже завершена.");

            var order = assignment.Order;
            if (order == null)
                return (false, null, "Заказ не найден.");

            var stopKind = assignment.OrderRouteStop?.Kind;
            if (!stopKind.HasValue)
            {
                var notes = assignment.Notes ?? string.Empty;
                if (notes.Contains("Доставка", StringComparison.OrdinalIgnoreCase))
                    stopKind = OrderRouteStopKind.RecipientDelivery;
                else if (notes.Contains("Забор", StringComparison.OrdinalIgnoreCase))
                    stopKind = OrderRouteStopKind.SenderPickup;
            }

            var isFinalDelivery = IsFinalDeliveryStop(assignment.Stage, stopKind);
            var isHubHandoff = IsHubHandoffStop(assignment.Stage, stopKind);

            assignment.Status = ShiftAssignmentStatus.Done;
            if (assignment.OrderRouteStop != null)
                assignment.OrderRouteStop.Status = OrderRouteStopStatus.Completed;

            var targetStatusName = isFinalDelivery
                ? "Доставлен"
                : isHubHandoff || stopKind == OrderRouteStopKind.SenderPickup
                    ? "В пути"
                    : "На выдаче";

            var targetStatusId = await ResolveStatusIdByNameAsync(targetStatusName);
            if (!targetStatusId.HasValue)
                return (false, null, $"Статус «{targetStatusName}» не настроен в системе.");

            var oldStatusId = order.Status_id;
            var hubHandoffBefore = order.HandoffStage;
            var wasDeliveredBefore = order.Delivered_at.HasValue;
            order.Status_id = targetStatusId.Value;
            ApplyMilestoneTimestamps(order, targetStatusName, targetStatusId.Value);
            var justDelivered = isFinalDelivery && !wasDeliveredBefore && order.Delivered_at.HasValue;
            string? deliveryWindowMismatchMessage = null;
            if (justDelivered)
                deliveryWindowMismatchMessage = ApplyDeliveryWindowComplianceFields(order);
            UpdateSlaFlags(order);
            order.Eta_at = EstimateEtaUtc(DateTime.UtcNow, order.Priority, order.Courier_id.HasValue && !isFinalDelivery);

            _context.OrderTimelineEvents.Add(new OrderTimelineEvent
            {
                Order_id = order.ID_Order,
                EventType = "ROUTE_STOP_COMPLETED",
                Title = "Точка маршрута выполнена",
                Message = $"Курьер #{courierProfileId} завершил точку: {assignment.OrderRouteStop?.Title ?? assignment.Notes ?? assignment.Stage.ToString()}.",
                OldStatus_id = oldStatusId,
                NewStatus_id = targetStatusId.Value,
                ActorUser_id = actorUserId
            });

            var hubHandoffTriggered = false;
            if (isHubHandoff || isFinalDelivery)
            {
                ApplyHandoffByStatusEvent(order, targetStatusName, targetStatusId.Value, assignment);
                hubHandoffTriggered = hubHandoffBefore != order.HandoffStage;
            }

            if (isFinalDelivery)
            {
                order.Plan_locked_shiftPlan_id = null;
                order.Plan_locked_at = null;
            }

            await SendStatusAutomationAsync(order, targetStatusId.Value);

            await _context.SaveChangesAsync();

            if (stopKind == OrderRouteStopKind.SenderPickup || isFinalDelivery)
                await SendCourierPickupDeliveryAlertsAsync(order, courierProfileId, stopKind == OrderRouteStopKind.SenderPickup, isFinalDelivery, actorUserId);
            if (!string.IsNullOrWhiteSpace(deliveryWindowMismatchMessage))
                await PublishDeliveryWindowMismatchAlertsAsync(order, deliveryWindowMismatchMessage!, actorUserId);
            try
            {
                await _shiftPlanner.RecalculateActivePlanDistanceAsync(courierProfileId);
            }
            catch
            {
                // Distance recalc must not break stop completion.
            }

            await PublishOrderEventAsync("order.route_stop_completed", order, new
            {
                assignmentId,
                courierProfileId,
                isFinalDelivery,
                isHubHandoff,
                targetStatusName
            });

            if (isHubHandoff || isFinalDelivery)
                await TryRebuildPlannerAsync(order.Company_id, "order.route_stop_completed");

            var statusEntity = await _context.OrderStatuses.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ID_OrderStatus == order.Status_id);

            return (true, new RouteStopCompletionResultDto
            {
                AssignmentId = assignment.ID_ShiftAssignment,
                OrderId = order.ID_Order,
                OrderNumber = order.Order_Number,
                NewStatusId = order.Status_id,
                NewStatusName = statusEntity?.Name ?? targetStatusName,
                OrderDelivered = isFinalDelivery,
                HubHandoffTriggered = hubHandoffTriggered,
                DeliveryWindowMismatch = !string.IsNullOrWhiteSpace(deliveryWindowMismatchMessage),
                DeliveryWindowWarning = deliveryWindowMismatchMessage
            }, null);
        }

        public async Task<IReadOnlyList<NearbyDeliveryStopDto>> GetNearbyDeliverableStopsAsync(
            int courierProfileId,
            double lat,
            double lon,
            double maxMeters = 15)
        {
            if (maxMeters <= 0)
                maxMeters = 15;

            var assignments = await _context.ShiftAssignments
                .Include(a => a.Order).ThenInclude(o => o!.DeliveryAddress)
                .Include(a => a.Order).ThenInclude(o => o!.PickupAddress)
                .Include(a => a.OrderRouteStop).ThenInclude(s => s!.Address)
                .Include(a => a.ShiftPlan)
                .Where(a =>
                    a.Order != null &&
                    a.Order.Delivered_at == null &&
                    (a.Status == ShiftAssignmentStatus.Pending || a.Status == ShiftAssignmentStatus.InProgress) &&
                    (a.ShiftPlan != null && a.ShiftPlan.Courier_id == courierProfileId || a.Order.Courier_id == courierProfileId))
                .ToListAsync();

            var nearby = new List<NearbyDeliveryStopDto>();
            foreach (var assignment in assignments)
            {
                var order = assignment.Order!;
                var stopKind = assignment.OrderRouteStop?.Kind;
                if (!stopKind.HasValue)
                {
                    var notes = assignment.Notes ?? string.Empty;
                    if (notes.Contains("Доставка", StringComparison.OrdinalIgnoreCase))
                        stopKind = OrderRouteStopKind.RecipientDelivery;
                    else if (notes.Contains("Забор", StringComparison.OrdinalIgnoreCase))
                        stopKind = OrderRouteStopKind.SenderPickup;
                }

                if (stopKind == OrderRouteStopKind.Hub)
                    continue;

                var isPickup = stopKind == OrderRouteStopKind.SenderPickup;
                var isDelivery = IsFinalDeliveryStop(assignment.Stage, stopKind);
                if (!isPickup && !isDelivery)
                    continue;

                if (!TryResolveAssignmentCoordinates(assignment, stopKind, out var stopLat, out var stopLon))
                    continue;

                var distanceM = HaversineMeters(lat, lon, stopLat, stopLon);
                if (distanceM > maxMeters)
                    continue;

                var title = assignment.OrderRouteStop?.Title?.Trim();
                if (string.IsNullOrWhiteSpace(title))
                    title = isPickup
                        ? "Забор у отправителя"
                        : "Доставка получателю";

                var addressLine = FormatAssignmentAddressLine(assignment, stopKind);

                nearby.Add(new NearbyDeliveryStopDto
                {
                    AssignmentId = assignment.ID_ShiftAssignment,
                    OrderId = order.ID_Order,
                    OrderNumber = order.Order_Number,
                    Title = title,
                    AddressLine = addressLine,
                    DistanceMeters = Math.Round(distanceM, 0),
                    StopKind = isPickup ? "Pickup" : "Delivery"
                });
            }

            var coveredOrderIds = nearby.Select(x => x.OrderId).ToHashSet();
            var directOrders = await _context.Orders
                .Include(o => o.DeliveryAddress)
                .Where(o =>
                    o.Courier_id == courierProfileId &&
                    o.Delivered_at == null &&
                    !coveredOrderIds.Contains(o.ID_Order))
                .ToListAsync();

            foreach (var order in directOrders)
            {
                var delivery = order.DeliveryAddress;
                if (delivery?.Latitude is not { } dLat || delivery.Longitude is not { } dLon || (dLat == 0 && dLon == 0))
                    continue;

                var distanceM = HaversineMeters(lat, lon, (double)dLat, (double)dLon);
                if (distanceM > maxMeters)
                    continue;

                var statusName = await _context.OrderStatuses.AsNoTracking()
                    .Where(s => s.ID_OrderStatus == order.Status_id)
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync();
                if (statusName != null &&
                    (statusName.Contains("Доставлен", StringComparison.OrdinalIgnoreCase) ||
                     statusName.Equals("Delivered", StringComparison.OrdinalIgnoreCase)))
                    continue;

                nearby.Add(new NearbyDeliveryStopDto
                {
                    AssignmentId = 0,
                    OrderId = order.ID_Order,
                    OrderNumber = order.Order_Number,
                    Title = "Доставка получателю",
                    AddressLine = JoinAddress(delivery),
                    DistanceMeters = Math.Round(distanceM, 0),
                    StopKind = "Delivery"
                });
            }

            return nearby
                .OrderBy(x => x.DistanceMeters)
                .ThenBy(x => x.OrderNumber)
                .ToList();
        }

        public async Task<OrderDispatchDto?> AutoDispatchAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.PickupAddress)
                .Include(o => o.DeliveryAddress)
                .FirstOrDefaultAsync(o => o.ID_Order == orderId);
            if (order == null)
                return null;

            var requiredWeightKg = Math.Max(order.Weight, ComputeVolumetricWeightKg(order.Length, order.Width, order.Height));
            var requiredVolumeM3 = ComputeVolumeM3(order.Length, order.Width, order.Height);
            var isIntercity = !string.Equals(order.PickupAddress?.City?.Trim(), order.DeliveryAddress?.City?.Trim(), StringComparison.OrdinalIgnoreCase);

            var candidates = await _context.CourierProfiles
                .AsNoTracking()
                .Where(c => c.Company_id == order.Company_id && c.Is_online)
                .Select(c => new
                {
                    c.ID_CourierProfile,
                    c.Current_lat,
                    c.Current_lon,
                    ActiveOrders = _context.Orders.Count(o => o.Courier_id == c.ID_CourierProfile && o.Delivered_at == null)
                })
                .ToListAsync();

            var candidateCourierIds = candidates.Select(c => c.ID_CourierProfile).Distinct().ToArray();
            var vehicles = await _context.Vehicles
                .AsNoTracking()
                .Where(v => v.Company_id == order.Company_id && v.CurrentCourier_id.HasValue && candidateCourierIds.Contains(v.CurrentCourier_id.Value))
                .ToListAsync();
            var vehicleByCourier = vehicles
                .GroupBy(v => v.CurrentCourier_id!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.ID_Vehicle).First());

            var filteredCandidates = new List<(int CourierId, decimal Lat, decimal Lon, int ActiveOrders, bool HasOperationalVehicle)>();
            foreach (var c in candidates)
            {
                if (!await IsCourierAllowedByZonesAsync(order, c.ID_CourierProfile))
                    continue;

                vehicleByCourier.TryGetValue(c.ID_CourierProfile, out var vehicle);
                var canHandleByVehicle = IsVehicleOperationalForDispatch(vehicle)
                    ? requiredWeightKg <= Math.Max(1m, vehicle!.Max_cargo_weight) && requiredVolumeM3 <= Math.Max(0.05m, vehicle.Cargo_volume)
                    : !isIntercity && requiredWeightKg <= 20m && requiredVolumeM3 <= 0.2m;

                if (!canHandleByVehicle)
                    continue;

                filteredCandidates.Add((c.ID_CourierProfile, c.Current_lat, c.Current_lon, c.ActiveOrders, IsVehicleOperationalForDispatch(vehicle)));
            }

            if (filteredCandidates.Count == 0)
                return null;

            var pickupLat = order.PickupAddress?.Latitude;
            var pickupLon = order.PickupAddress?.Longitude;

            var scored = filteredCandidates
                .Select(c =>
                {
                    decimal? distance = null;
                    if (pickupLat.HasValue && pickupLon.HasValue)
                        distance = (decimal)HaversineKm((double)c.Lat, (double)c.Lon, (double)pickupLat.Value, (double)pickupLon.Value);

                    var distanceScore = (double)(distance ?? 30m);
                    var loadScore = c.ActiveOrders * 4.0;
                    var urgentBoost = order.Priority switch
                    {
                        3 => -5.5,
                        2 => -3.0,
                        1 => -1.5,
                        _ => 0.0
                    };
                    var vehicleBoost = c.HasOperationalVehicle ? -2.0 : 2.0;
                    var total = distanceScore + loadScore + urgentBoost + vehicleBoost;

                    return new
                    {
                        ID_CourierProfile = c.CourierId,
                        c.ActiveOrders,
                        DistanceKm = distance,
                        Score = total,
                        c.HasOperationalVehicle
                    };
                })
                .OrderBy(x => x.Score)
                .ToList();

            var winner = scored.First();
            var winnerName = await GetCourierDisplayNameAsync(winner.ID_CourierProfile);
            var oldCourierId = order.Courier_id;
            order.Courier_id = winner.ID_CourierProfile;
            order.Eta_at = EstimateEtaUtc(DateTime.UtcNow, order.Priority, hasCourierAssigned: true);

            var isSlaRisk = IsSlaRisk(order);
            _context.OrderTimelineEvents.Add(new OrderTimelineEvent
            {
                Order_id = order.ID_Order,
                EventType = "AUTO_DISPATCH",
                Title = "Авто-диспетчеризация",
                Message = $"Автоназначение {winnerName} (дистанция: {winner.DistanceKm?.ToString("0.0") ?? "n/a"} км, активных заказов: {winner.ActiveOrders}, ТС: {(winner.HasOperationalVehicle ? "доступно" : "без ТС")}).",
                OldCourier_id = oldCourierId,
                NewCourier_id = winner.ID_CourierProfile
            });

            if (isSlaRisk)
            {
                _context.OrderTimelineEvents.Add(new OrderTimelineEvent
                {
                    Order_id = order.ID_Order,
                    EventType = "SLA_RISK",
                    Title = "Риск SLA",
                    Message = "Заказ отмечен как риск срыва SLA на этапе автоназначения."
                });
                await CreateSlaRiskAlertsAsync(order);
            }

            await _context.SaveChangesAsync();
            await PublishOrderEventAsync("order.auto_dispatched", order, new
            {
                oldCourierId,
                newCourierId = winner.ID_CourierProfile,
                winner.DistanceKm,
                winner.ActiveOrders,
                winner.HasOperationalVehicle,
                isSlaRisk
            });
            await TryRebuildPlannerAsync(order.Company_id, "order.auto_dispatch");

            return new OrderDispatchDto
            {
                OrderId = order.ID_Order,
                CourierId = winner.ID_CourierProfile,
                DistanceKm = winner.DistanceKm,
                ActiveOrders = winner.ActiveOrders,
                IsSlaRisk = isSlaRisk,
                EtaAt = order.Eta_at,
                DecisionReason = "Минимальный совокупный score: дистанция + текущая загрузка + приоритет SLA + доступность ТС."
            };
        }

        public async Task<IReadOnlyList<OrderTimelineEvent>> GetTimelineAsync(int orderId)
        {
            return await _context.OrderTimelineEvents
                .Where(e => e.Order_id == orderId)
                .OrderBy(e => e.Created_at)
                .ToListAsync();
        }

        public async Task<OrderEtaDto?> GetEtaAsync(int orderId)
        {
            var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.ID_Order == orderId);
            if (order == null)
                return null;

            var risk = IsSlaRisk(order);
            var (windowFrom, windowTo) = GetDeliveryWindowUtc(order);
            return new OrderEtaDto
            {
                OrderId = order.ID_Order,
                EtaAtUtc = order.Eta_at,
                SlaDueAtUtc = order.Sla_due_at,
                IsSlaBreached = order.Sla_breached_at.HasValue || (order.Sla_due_at.HasValue && DateTime.UtcNow > order.Sla_due_at.Value),
                IsSlaRisk = risk,
                DelayReason = order.Delay_reason,
                DeliveryWindowFromUtc = windowFrom,
                DeliveryWindowToUtc = windowTo,
                DeliveryWindowText = DeliveryWindowPolicy.FormatDisplayText(
                    order.Priority, order.Sla_due_at, order.Eta_at, order.Created_at)
            };
        }

        private static decimal CalculateEstimatedCost(
            decimal distanceKm,
            decimal weightKg,
            decimal lengthCm,
            decimal widthCm,
            decimal heightCm,
            byte priority,
            DeliveryRouteKind routeKind,
            DateTime createdAtUtc,
            decimal fuelCostRub)
        {
            var baseFare = 180m;
            var perKm = routeKind switch
            {
                DeliveryRouteKind.DirectIntercity => 30m,
                DeliveryRouteKind.ViaHub => 24m,
                _ => 20m
            };
            var safeDistance = Math.Max(1m, distanceKm);
            var volumetricKg = Math.Max(0m, (lengthCm * widthCm * heightCm) / 5000m);
            var chargeableWeight = Math.Max(Math.Max(0.1m, weightKg), volumetricKg);
            var weightSurcharge = Math.Max(0m, chargeableWeight - 3m) * 15m;

            var priorityMul = priority switch
            {
                2 => 1.55m,
                1 => 1.25m,
                _ => 1.00m
            };

            // В пиковые часы дороже, в дневное окно будней — дешевле.
            var localHour = createdAtUtc.Hour;
            var dayOfWeek = createdAtUtc.DayOfWeek;
            var timeMul = 1.00m;
            if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                timeMul += 0.08m;
            if (localHour >= 18 && localHour < 22)
                timeMul += 0.12m;
            else if (localHour >= 10 && localHour < 17 && dayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday)
                timeMul -= 0.07m;

            var subtotal = baseFare + safeDistance * perKm + weightSurcharge + Math.Max(0m, fuelCostRub);
            var result = subtotal * priorityMul * timeMul;
            return Math.Round(Math.Max(120m, result), 2);
        }

        private async Task<decimal> EstimateFuelCostRubAsync(
            int companyId,
            int? courierProfileId,
            DeliveryRouteKind routeKind,
            decimal distanceKm)
        {
            var safeDistance = Math.Max(1m, distanceKm);
            var defaultConsumption = routeKind switch
            {
                DeliveryRouteKind.LocalUrban => 10.5m,
                DeliveryRouteKind.ViaHub => 9.6m,
                DeliveryRouteKind.DirectIntercity => 8.7m,
                _ => 10.0m
            };

            var consumptionL100 = defaultConsumption;
            var fuelPriceRubPerLiter = await _fuelPriceService.GetPriceRubPerLiterAsync(null, cancellationToken: CancellationToken.None);

            if (courierProfileId.HasValue)
            {
                var vehicle = await _context.Vehicles
                    .AsNoTracking()
                    .Include(v => v.VehicleModel)
                    .Include(v => v.FuelType)
                    .Where(v => v.Company_id == companyId && v.CurrentCourier_id == courierProfileId.Value)
                    .OrderByDescending(v => v.ID_Vehicle)
                    .FirstOrDefaultAsync();

                if (vehicle?.VehicleModel != null)
                {
                    var modelConsumption = routeKind == DeliveryRouteKind.LocalUrban
                        ? vehicle.VehicleModel.AvgFuelCity
                        : vehicle.VehicleModel.AvgFuelHighWay;
                    if (modelConsumption > 0m)
                        consumptionL100 = modelConsumption;
                }

                fuelPriceRubPerLiter = await _fuelPriceService.GetPriceRubPerLiterAsync(vehicle?.FuelType?.Name);
            }

            var liters = safeDistance * consumptionL100 / 100m;
            return Math.Round(Math.Max(0m, liters * fuelPriceRubPerLiter), 2);
        }

        private static decimal EstimateRouteDistanceKm(
            DeliveryRouteKind routeKind,
            Address pickup,
            Address delivery,
            LogisticsHub? originHub,
            LogisticsHub? destinationHub)
        {
            decimal Segment(Address? a, Address? b)
            {
                if (a?.Latitude is not null && a.Longitude is not null && b?.Latitude is not null && b.Longitude is not null)
                    return (decimal)HaversineKm((double)a.Latitude.Value, (double)a.Longitude.Value, (double)b.Latitude.Value, (double)b.Longitude.Value);
                if (!string.IsNullOrWhiteSpace(a?.City) && !string.IsNullOrWhiteSpace(b?.City) &&
                    string.Equals(a.City, b.City, StringComparison.OrdinalIgnoreCase))
                    return 8m;
                return 35m;
            }

            return routeKind switch
            {
                DeliveryRouteKind.ViaHub when originHub?.Address != null && destinationHub?.Address != null =>
                    Segment(pickup, originHub.Address) + Segment(originHub.Address, destinationHub.Address) + Segment(destinationHub.Address, delivery),
                _ => Segment(pickup, delivery)
            };
        }

        private async Task<RouteChoice> ResolveRouteChoiceAsync(CreateOrderRequest request, int companyId, Address pickup, Address delivery)
        {
            if (!request.AutoSelectRouteKind)
            {
                if (!Enum.IsDefined(typeof(DeliveryRouteKind), request.DeliveryRouteKind))
                    throw new InvalidOperationException("Некорректный тип маршрута доставки.");

                var manualKind = (DeliveryRouteKind)request.DeliveryRouteKind;
                var (originHub, destinationHub) = await ResolveManualHubsAsync(request, companyId, manualKind);
                return new RouteChoice(manualKind, originHub, destinationHub);
            }

            var requiredWeightKg = Math.Max(request.Weight, ComputeVolumetricWeightKg(request.Length, request.Width, request.Height));
            var requiredVolumeM3 = ComputeVolumeM3(request.Length, request.Width, request.Height);
            var intercity = !string.Equals(pickup.City?.Trim(), delivery.City?.Trim(), StringComparison.OrdinalIgnoreCase);

            var hubs = await _context.LogisticsHubs
                .Include(h => h.Address)
                .Where(h => h.Company_id == companyId)
                .ToListAsync();
            var (originHubAuto, destinationHubAuto) = ResolveBestHubsForRoute(hubs, pickup, delivery);
            var resources = await BuildRouteResourceSnapshotAsync(companyId);
            var demand = await BuildRouteDemandSnapshotAsync(companyId, pickup, delivery);

            var candidates = new List<RouteChoiceScore>();

            // Local city leg: cheap and fast for intra-city shipments.
            candidates.Add(new RouteChoiceScore(
                new RouteChoice(DeliveryRouteKind.LocalUrban, null, null),
                ScoreRouteCandidate(DeliveryRouteKind.LocalUrban, pickup, delivery, null, null, requiredWeightKg, requiredVolumeM3, intercity, resources, demand)));

            // Direct intercity: single-leg long haul without hub transfer.
            candidates.Add(new RouteChoiceScore(
                new RouteChoice(DeliveryRouteKind.DirectIntercity, null, null),
                ScoreRouteCandidate(DeliveryRouteKind.DirectIntercity, pickup, delivery, null, null, requiredWeightKg, requiredVolumeM3, intercity, resources, demand)));

            if (originHubAuto != null && destinationHubAuto != null)
            {
                candidates.Add(new RouteChoiceScore(
                    new RouteChoice(DeliveryRouteKind.ViaHub, originHubAuto, destinationHubAuto),
                    ScoreRouteCandidate(DeliveryRouteKind.ViaHub, pickup, delivery, originHubAuto, destinationHubAuto, requiredWeightKg, requiredVolumeM3, intercity, resources, demand)));
            }

            var winner = candidates
                .OrderBy(c => c.Score)
                .First()
                .Choice;

            return winner;
        }

        private async Task<(LogisticsHub? originHub, LogisticsHub? destinationHub)> ResolveManualHubsAsync(CreateOrderRequest request, int companyId, DeliveryRouteKind kind)
        {
            if (kind != DeliveryRouteKind.ViaHub)
                return (null, null);

            if (!request.OriginHub_id.HasValue || !request.DestinationHub_id.HasValue)
                throw new InvalidOperationException("Для доставки через хабы укажите склад отправления и склад назначения.");

            var originHub = await _context.LogisticsHubs
                .Include(h => h.Address)
                .FirstOrDefaultAsync(h => h.ID_LogisticsHub == request.OriginHub_id && h.Company_id == companyId);
            var destinationHub = await _context.LogisticsHubs
                .Include(h => h.Address)
                .FirstOrDefaultAsync(h => h.ID_LogisticsHub == request.DestinationHub_id && h.Company_id == companyId);
            if (originHub == null || destinationHub == null)
                throw new InvalidOperationException("Один из складов не найден или принадлежит другой компании.");

            return (originHub, destinationHub);
        }

        private static (LogisticsHub? originHub, LogisticsHub? destinationHub) ResolveBestHubsForRoute(List<LogisticsHub> hubs, Address pickup, Address delivery)
        {
            if (hubs.Count == 0)
                return (null, null);

            LogisticsHub? origin = null;
            LogisticsHub? destination = null;
            var pickupPoint = TryGetPoint(pickup);
            var deliveryPoint = TryGetPoint(delivery);

            if (pickupPoint.HasValue)
                origin = hubs.OrderBy(h => DistPointToAddressKm(pickupPoint.Value, h.Address)).FirstOrDefault();
            else
                origin = hubs.FirstOrDefault();

            if (deliveryPoint.HasValue)
            {
                destination = hubs
                    .OrderBy(h => DistPointToAddressKm(deliveryPoint.Value, h.Address))
                    .ThenBy(h => h.ID_LogisticsHub == origin?.ID_LogisticsHub ? 1 : 0)
                    .FirstOrDefault();
            }
            else
            {
                destination = hubs.FirstOrDefault(h => h.ID_LogisticsHub != origin?.ID_LogisticsHub) ?? origin;
            }

            return (origin, destination ?? origin);
        }

        private static decimal ScoreRouteCandidate(
            DeliveryRouteKind kind,
            Address pickup,
            Address delivery,
            LogisticsHub? originHub,
            LogisticsHub? destinationHub,
            decimal requiredWeightKg,
            decimal requiredVolumeM3,
            bool intercity,
            RouteResourceSnapshot resources,
            RouteDemandSnapshot demand)
        {
            var distance = EstimateRouteDistanceKm(kind, pickup, delivery, originHub, destinationHub);
            decimal score = distance;

            var requiresVehicle = kind is DeliveryRouteKind.DirectIntercity or DeliveryRouteKind.ViaHub
                                  || requiredWeightKg >= 80m
                                  || requiredVolumeM3 >= 1.5m;

            if (resources.OnlineCourierCount == 0)
                score += 6000m;

            if (requiresVehicle)
            {
                if (resources.OperationalVehicleCount == 0)
                    score += 4500m;
                else if (!resources.HasFittingOperationalVehicle(requiredWeightKg, requiredVolumeM3))
                    score += 2600m;
                else
                    score += 90m / Math.Max(1, resources.OperationalVehicleCount);
            }
            else
            {
                score += 40m / Math.Max(1, resources.OnlineCourierCount);
            }

            if (intercity)
            {
                if (kind == DeliveryRouteKind.LocalUrban)
                    score += 1200m;
                if (kind == DeliveryRouteKind.ViaHub)
                    score -= 35m;
            }
            else if (kind == DeliveryRouteKind.DirectIntercity)
            {
                score += 80m;
            }

            // Consolidation logic: when many active orders converge to same city,
            // prefer hub-flow so first-mile and long-haul legs can be split among different couriers.
            if (intercity)
            {
                if (kind == DeliveryRouteKind.ViaHub)
                {
                    score -= demand.DestinationCityBacklogCount * 18m;
                    score -= demand.DestinationCityIntercityBacklogCount * 26m;
                    score -= demand.OriginCityPickupBacklogCount * 8m;
                }
                else if (kind == DeliveryRouteKind.DirectIntercity)
                {
                    score += demand.DestinationCityBacklogCount * 7m;
                    score += demand.DestinationCityIntercityBacklogCount * 12m;
                }
            }

            if (kind == DeliveryRouteKind.ViaHub && (originHub == null || destinationHub == null))
                score += 5000m;

            return score;
        }

        private async Task<RouteResourceSnapshot> BuildRouteResourceSnapshotAsync(int companyId)
        {
            var onlineCouriers = await _context.CourierProfiles
                .AsNoTracking()
                .Where(c => c.Company_id == companyId && c.Is_online)
                .Select(c => c.ID_CourierProfile)
                .ToListAsync();

            var vehicles = await _context.Vehicles
                .AsNoTracking()
                .Where(v => v.Company_id == companyId && v.CurrentCourier_id.HasValue && onlineCouriers.Contains(v.CurrentCourier_id.Value))
                .ToListAsync();

            var operational = vehicles.Where(IsVehicleOperationalForDispatch).ToList();

            return new RouteResourceSnapshot(onlineCouriers.Count, operational);
        }

        private async Task<RouteDemandSnapshot> BuildRouteDemandSnapshotAsync(int companyId, Address pickup, Address delivery)
        {
            var targetDeliveryCity = NormalizeCity(delivery.City);
            var targetPickupCity = NormalizeCity(pickup.City);
            if (string.IsNullOrWhiteSpace(targetDeliveryCity))
                return new RouteDemandSnapshot(0, 0, 0);

            var activeOrders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.PickupAddress)
                .Include(o => o.DeliveryAddress)
                .Where(o => o.Company_id == companyId && o.Delivered_at == null)
                .ToListAsync();

            var sameDestinationCity = activeOrders
                .Where(o => string.Equals(NormalizeCity(o.DeliveryAddress?.City), targetDeliveryCity, StringComparison.Ordinal))
                .ToList();

            var intercityToDestination = sameDestinationCity
                .Count(o => !string.Equals(NormalizeCity(o.PickupAddress?.City), targetDeliveryCity, StringComparison.Ordinal));

            var sameOriginCity = string.IsNullOrWhiteSpace(targetPickupCity)
                ? 0
                : activeOrders.Count(o => string.Equals(NormalizeCity(o.PickupAddress?.City), targetPickupCity, StringComparison.Ordinal));

            return new RouteDemandSnapshot(
                DestinationCityBacklogCount: sameDestinationCity.Count,
                DestinationCityIntercityBacklogCount: intercityToDestination,
                OriginCityPickupBacklogCount: sameOriginCity);
        }

        private static string BuildRouteLabel(DeliveryRouteKind kind, LogisticsHub? originHub, LogisticsHub? destinationHub)
        {
            return kind switch
            {
                DeliveryRouteKind.LocalUrban => "Городской (LocalUrban)",
                DeliveryRouteKind.DirectIntercity => "Прямой межгород (DirectIntercity)",
                DeliveryRouteKind.ViaHub => $"Через хабы (ViaHub): {originHub?.Name ?? "—"} -> {destinationHub?.Name ?? "—"}",
                _ => kind.ToString()
            };
        }

        private static (double lat, double lon)? TryGetPoint(Address? address)
        {
            if (address?.Latitude is not { } lat || address.Longitude is not { } lon)
                return null;
            return ((double)lat, (double)lon);
        }

        private static decimal DistPointToAddressKm((double lat, double lon) point, Address? address)
        {
            var other = TryGetPoint(address);
            if (!other.HasValue)
                return 999m;
            return (decimal)HaversineKm(point.lat, point.lon, other.Value.lat, other.Value.lon);
        }

        private static string NormalizeCity(string? city)
            => string.IsNullOrWhiteSpace(city) ? string.Empty : city.Trim().ToLowerInvariant();


        private static DateTime EstimateEtaUtc(DateTime nowUtc, byte priority, bool hasCourierAssigned)
        {
            var baseMinutes = priority switch
            {
                3 => 28,
                2 => 45,
                1 => 90,
                _ => 150
            };

            if (!hasCourierAssigned)
                baseMinutes += 30;

            return nowUtc.AddMinutes(baseMinutes);
        }

        private static bool TryResolveAssignmentCoordinates(
            ShiftAssignment assignment,
            OrderRouteStopKind? stopKind,
            out double lat,
            out double lon)
        {
            lat = 0;
            lon = 0;

            var routeAddr = assignment.OrderRouteStop?.Address;
            if (routeAddr?.Latitude is { } rLat && routeAddr.Longitude is { } rLon && (rLat != 0 || rLon != 0))
            {
                lat = (double)rLat;
                lon = (double)rLon;
                return true;
            }

            var order = assignment.Order;
            if (order == null)
                return false;

            var notes = assignment.Notes ?? string.Empty;
            if (!stopKind.HasValue)
            {
                if (notes.Contains("забор", StringComparison.OrdinalIgnoreCase))
                    stopKind = OrderRouteStopKind.SenderPickup;
                else if (notes.Contains("доставк", StringComparison.OrdinalIgnoreCase))
                    stopKind = OrderRouteStopKind.RecipientDelivery;
            }

            if (stopKind == OrderRouteStopKind.SenderPickup)
            {
                var pickup = order.PickupAddress;
                if (pickup?.Latitude is { } pLat && pickup.Longitude is { } pLon && (pLat != 0 || pLon != 0))
                {
                    lat = (double)pLat;
                    lon = (double)pLon;
                    return true;
                }
            }

            if (notes.Contains("забор", StringComparison.OrdinalIgnoreCase))
            {
                var pickup = order.PickupAddress;
                if (pickup?.Latitude is { } pLat2 && pickup.Longitude is { } pLon2 && (pLat2 != 0 || pLon2 != 0))
                {
                    lat = (double)pLat2;
                    lon = (double)pLon2;
                    return true;
                }
            }

            var delivery = order.DeliveryAddress;
            if (delivery?.Latitude is { } dLat && delivery.Longitude is { } dLon && (dLat != 0 || dLon != 0))
            {
                lat = (double)dLat;
                lon = (double)dLon;
                return true;
            }

            return false;
        }

        private static string FormatAssignmentAddressLine(ShiftAssignment assignment, OrderRouteStopKind? stopKind)
        {
            var routeAddr = assignment.OrderRouteStop?.Address;
            if (routeAddr != null)
            {
                var line = JoinAddress(routeAddr);
                if (!string.IsNullOrWhiteSpace(line))
                    return line;
            }

            if (stopKind == OrderRouteStopKind.SenderPickup)
            {
                var pickup = assignment.Order?.PickupAddress;
                if (pickup != null)
                {
                    var line = JoinAddress(pickup);
                    if (!string.IsNullOrWhiteSpace(line))
                        return line;
                }
            }

            var delivery = assignment.Order?.DeliveryAddress;
            if (delivery != null)
            {
                var line = JoinAddress(delivery);
                if (!string.IsNullOrWhiteSpace(line))
                    return line;
            }

            return assignment.Notes?.Trim() ?? "—";
        }

        private static string JoinAddress(Address address)
        {
            var parts = new[]
            {
                address.City?.Trim(),
                address.Street?.Trim(),
                address.House?.Trim(),
                address.Flat?.Trim()
            }.Where(p => !string.IsNullOrWhiteSpace(p));
            var line = string.Join(", ", parts);
            return string.IsNullOrWhiteSpace(line) ? string.Empty : line;
        }

        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
            => HaversineKm(lat1, lon1, lat2, lon2) * 1000.0;

        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double DegreesToRadians(double deg) => deg * (Math.PI / 180.0);

        private static decimal ComputeVolumetricWeightKg(decimal lengthCm, decimal widthCm, decimal heightCm)
            => Math.Max(0m, (lengthCm * widthCm * heightCm) / 5000m);

        private static decimal ComputeVolumeM3(decimal lengthCm, decimal widthCm, decimal heightCm)
        {
            var l = Math.Max(0m, lengthCm) / 100m;
            var w = Math.Max(0m, widthCm) / 100m;
            var h = Math.Max(0m, heightCm) / 100m;
            return l * w * h;
        }

        private static bool IsVehicleOperationalForDispatch(Vehicle? vehicle)
        {
            if (vehicle == null || !vehicle.Is_available)
                return false;

            var now = DateTime.UtcNow;
            if (vehicle.Maintenance_due_at.HasValue && vehicle.Maintenance_due_at.Value <= now)
                return false;
            if (vehicle.Insurance_expires_at.HasValue && vehicle.Insurance_expires_at.Value <= now)
                return false;
            if (vehicle.Registration_expires_at.HasValue && vehicle.Registration_expires_at.Value <= now)
                return false;
            return true;
        }

        private sealed record RouteChoice(DeliveryRouteKind RouteKind, LogisticsHub? OriginHub, LogisticsHub? DestinationHub);

        private sealed record RouteChoiceScore(RouteChoice Choice, decimal Score);

        private sealed class RouteResourceSnapshot
        {
            private readonly List<Vehicle> _operationalVehicles;

            public RouteResourceSnapshot(int onlineCourierCount, List<Vehicle> operationalVehicles)
            {
                OnlineCourierCount = onlineCourierCount;
                _operationalVehicles = operationalVehicles;
            }

            public int OnlineCourierCount { get; }
            public int OperationalVehicleCount => _operationalVehicles.Count;

            public bool HasFittingOperationalVehicle(decimal requiredWeightKg, decimal requiredVolumeM3)
            {
                return _operationalVehicles.Any(v =>
                    Math.Max(1m, v.Max_cargo_weight) >= requiredWeightKg &&
                    Math.Max(0.05m, v.Cargo_volume) >= requiredVolumeM3);
            }
        }

        private sealed record RouteDemandSnapshot(
            int DestinationCityBacklogCount,
            int DestinationCityIntercityBacklogCount,
            int OriginCityPickupBacklogCount);

        private static void ApplyMilestoneTimestamps(Order order, string? statusName, int statusId)
        {
            var name = statusName?.Trim() ?? string.Empty;
            if ((name.Equals("Принят", StringComparison.OrdinalIgnoreCase)
                 || name.Equals("Ожидает курьера", StringComparison.OrdinalIgnoreCase))
                && !order.Pickup_started_at.HasValue)
                order.Pickup_started_at = DateTime.UtcNow;

            if (name.Equals("В пути", StringComparison.OrdinalIgnoreCase) && !order.In_transit_at.HasValue)
                order.In_transit_at = DateTime.UtcNow;

            if (name.Equals("На выдаче", StringComparison.OrdinalIgnoreCase) && !order.Arrived_at.HasValue)
                order.Arrived_at = DateTime.UtcNow;

            if (name.Equals("Доставлен", StringComparison.OrdinalIgnoreCase) && !order.Delivered_at.HasValue)
                order.Delivered_at = DateTime.UtcNow;
        }

        private static bool CanRevokeCourierFromOrder(Order order, string? statusName)
        {
            if (order.Delivered_at.HasValue)
                return false;

            var name = statusName?.Trim() ?? "";
            if (name.Length == 0)
                return true;

            if (name.Contains("Отмен", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("cancel", StringComparison.OrdinalIgnoreCase))
                return false;

            if (name.Contains("Доставлен", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private static bool CanClientDeleteOrder(Order order, string? statusName)
        {
            if (order.Courier_id.HasValue || order.Delivered_at.HasValue ||
                order.Pickup_started_at.HasValue || order.In_transit_at.HasValue || order.Arrived_at.HasValue)
                return false;

            if (OrderStatusRules.IsCancelled(statusName) || OrderStatusRules.IsDelivered(statusName, order.Delivered_at))
                return false;

            var name = statusName?.Trim() ?? string.Empty;
            if (name.Length == 0)
                return true;

            if (name.Contains("нов", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("созда", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("ожида", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static bool IsFinalDeliveryStop(ShiftAssignmentStage stage, OrderRouteStopKind? kind)
            => kind == OrderRouteStopKind.RecipientDelivery
               || (stage == ShiftAssignmentStage.HubToRecipient && kind != OrderRouteStopKind.Hub);

        private static bool IsHubHandoffStop(ShiftAssignmentStage stage, OrderRouteStopKind? kind)
            => kind == OrderRouteStopKind.Hub
               && stage is ShiftAssignmentStage.PickupToHub or ShiftAssignmentStage.HubToHub;

        private async Task<int?> ResolveStatusIdByNameAsync(string statusName)
        {
            return await _context.OrderStatuses.AsNoTracking()
                .Where(s => s.Name == statusName)
                .Select(s => (int?)s.ID_OrderStatus)
                .FirstOrDefaultAsync();
        }

        private void ApplyHandoffByStatusEvent(Order order, string? statusName, int statusId, ShiftAssignment? activeAssignment)
        {
            if (order.DeliveryRouteKind != DeliveryRouteKind.ViaHub || activeAssignment == null)
                return;

            var stage = activeAssignment.Stage;
            var name = statusName?.Trim() ?? string.Empty;
            var shouldAdvanceLeg = name.Equals("В пути", StringComparison.OrdinalIgnoreCase)
                                   || name.Equals("На выдаче", StringComparison.OrdinalIgnoreCase);
            var isFinalDelivery = name.Equals("Доставлен", StringComparison.OrdinalIgnoreCase);
            var changed = false;

            if (stage == ShiftAssignmentStage.PickupToHub && shouldAdvanceLeg)
            {
                order.HandoffStage = OrderHandoffStage.AtHub;
                order.Courier_id = null;
                order.Plan_locked_shiftPlan_id = null;
                order.Plan_locked_at = null;
                activeAssignment.Status = ShiftAssignmentStatus.Done;
                changed = true;

                _context.OrderTimelineEvents.Add(new OrderTimelineEvent
                {
                    Order_id = order.ID_Order,
                    EventType = "HANDOFF_STAGE",
                    Title = "Передача на хаб отправления",
                    Message = "Заказ доставлен в хаб и готов к следующему плечу."
                });
            }
            else if (stage == ShiftAssignmentStage.HubToHub && shouldAdvanceLeg)
            {
                order.HandoffStage = OrderHandoffStage.LastMileInProgress;
                order.Courier_id = null;
                order.Plan_locked_shiftPlan_id = null;
                order.Plan_locked_at = null;
                activeAssignment.Status = ShiftAssignmentStatus.Done;
                changed = true;

                _context.OrderTimelineEvents.Add(new OrderTimelineEvent
                {
                    Order_id = order.ID_Order,
                    EventType = "HANDOFF_STAGE",
                    Title = "Прибытие в хаб назначения",
                    Message = "Заказ прибыл в хаб назначения и ожидает курьера последней мили."
                });
            }
            else if (stage == ShiftAssignmentStage.HubToRecipient && isFinalDelivery)
            {
                order.HandoffStage = OrderHandoffStage.Completed;
                activeAssignment.Status = ShiftAssignmentStatus.Done;
                changed = true;
            }

            if (!changed)
            {
                if (shouldAdvanceLeg && stage != ShiftAssignmentStage.HubToRecipient)
                    activeAssignment.Status = ShiftAssignmentStatus.Done;
                else if (isFinalDelivery)
                    activeAssignment.Status = ShiftAssignmentStatus.Done;
            }
        }

        private static void UpdateSlaFlags(Order order)
        {
            if (order.Sla_due_at.HasValue && DateTime.UtcNow > order.Sla_due_at.Value && !order.Sla_breached_at.HasValue)
                order.Sla_breached_at = DateTime.UtcNow;
        }

        private static bool IsSlaRisk(Order order)
        {
            if (!order.Sla_due_at.HasValue || !order.Eta_at.HasValue)
                return false;
            return order.Eta_at.Value > order.Sla_due_at.Value;
        }

        private async Task<bool> IsCourierAllowedByZonesAsync(Order order, int courierProfileId)
        {
            var zonesExist = await _context.ServiceAreaZones
                .AsNoTracking()
                .AnyAsync(z => z.Company_id == order.Company_id && z.Is_active);
            if (!zonesExist)
                return true;

            var lat = order.PickupAddress?.Latitude;
            var lon = order.PickupAddress?.Longitude;
            // Без координат забора нельзя проверить попадание в зону — не блокируем ручное назначение.
            if (!lat.HasValue || !lon.HasValue)
                return true;

            var zones = await _context.ServiceAreaZoneCouriers
                .AsNoTracking()
                .Where(zc => zc.CourierProfile_id == courierProfileId && zc.Zone.Company_id == order.Company_id && zc.Zone.Is_active)
                .Select(zc => new
                {
                    zc.Zone.Center_lat,
                    zc.Zone.Center_lon,
                    zc.Zone.Radius_km
                })
                .ToListAsync();

            // Курьер без привязки к зонам: не блокируем ручное назначение (иначе при активных зонах компании
            // назначить «вне матрицы» было невозможно).
            if (zones.Count == 0)
                return true;

            foreach (var zone in zones)
            {
                var distance = HaversineKm((double)zone.Center_lat, (double)zone.Center_lon, (double)lat.Value, (double)lon.Value);
                if ((decimal)distance <= zone.Radius_km)
                    return true;
            }

            return false;
        }

        private async Task CreateSlaRiskAlertsAsync(Order order)
        {
            var typeId = await _context.NotificationTypes
                .AsNoTracking()
                .Where(t => t.Name.ToLower().Contains("sla") || t.Name.ToLower().Contains("важн"))
                .Select(t => t.ID_NotificationType)
                .FirstOrDefaultAsync();

            if (typeId == 0)
            {
                typeId = await _context.NotificationTypes
                    .AsNoTracking()
                    .Select(t => t.ID_NotificationType)
                    .FirstOrDefaultAsync();
                if (typeId == 0)
                    return;
            }

            var receivers = await _context.Users
                .AsNoTracking()
                .Where(u => u.Company_id == order.Company_id &&
                           (u.Role.Name == "Менеджер" || u.Role.Name == "Логист"))
                .Select(u => u.ID_User)
                .ToListAsync();

            if (receivers.Count == 0)
                return;

            foreach (var userId in receivers)
            {
                _context.Notifications.Add(new Notification
                {
                    Company_id = order.Company_id,
                    User_id = userId,
                    Type_id = typeId,
                    Title = "Риск срыва SLA",
                    Message = $"Заказ #{order.Order_Number} имеет риск нарушения SLA.",
                    Order_id = order.ID_Order,
                    Is_read = false,
                    Sent_at = DateOnly.FromDateTime(DateTime.UtcNow)
                });
            }
        }

        private async Task SendStatusAutomationAsync(Order order, int statusId)
        {
            var template = await _templateService.ResolveForOrderStatusAsync(order.Company_id, statusId);
            if (template == null)
                return;

            var statusName = await _context.OrderStatuses
                .AsNoTracking()
                .Where(s => s.ID_OrderStatus == statusId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();

            var title = _templateService.Render(template.TitleTemplate, order, statusName);
            var body = _templateService.Render(template.BodyTemplate, order, statusName);
            if (TryBuildCustomerRealtimeMessage(statusName, order.Order_Number, out var rtTitle, out var rtBody))
            {
                title = rtTitle;
                body = rtBody;
            }

            var clientUserId = await _context.ClientProfiles
                .AsNoTracking()
                .Where(c => c.ID_ClientProfile == order.Client_id)
                .Select(c => c.User_id)
                .FirstOrDefaultAsync();

            if (clientUserId > 0)
            {
                var typeId = await ResolveNotificationTypeIdAsync();
                if (typeId > 0)
                    await _notificationService.SendAsync(clientUserId, typeId, title, body, order.ID_Order, priority: order.Priority, isCritical: order.Priority >= 2, requiresAck: order.Priority >= 2);
            }

            var chatRoomId = await _context.ChatRooms
                .AsNoTracking()
                .Where(cr => cr.Order_id == order.ID_Order)
                .Select(cr => cr.ID_ChatRoom)
                .FirstOrDefaultAsync();

            if (chatRoomId > 0)
            {
                var systemSenderId = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Company_id == order.Company_id && (u.Role.Name == "Менеджер" || u.Role.Name == "Админ" || u.Role.Name == "Администратор"))
                    .Select(u => u.ID_User)
                    .FirstOrDefaultAsync();
                if (systemSenderId > 0)
                {
                    var msg = new ChatMessage
                    {
                        ChatRoom_id = chatRoomId,
                        Sender_id = systemSenderId,
                        MessageText = body,
                        Sent_at = DateTime.UtcNow,
                        Is_deleted = false
                    };
                    _context.ChatMessages.Add(msg);
                    await _context.SaveChangesAsync();

                    await _hubContext.Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("ReceiveMessage", new
                    {
                        id = msg.ID_ChatMessage,
                        chatRoomId,
                        senderId = systemSenderId,
                        senderName = "System",
                        messageText = body,
                        attachmentUrl = (string?)null,
                        sentAt = msg.Sent_at
                    });
                }
            }
        }

        private async Task<int> ResolveNotificationTypeIdAsync()
        {
            var id = await _context.NotificationTypes
                .AsNoTracking()
                .Where(t => t.Name.ToLower().Contains("статус") || t.Name.ToLower().Contains("заказ"))
                .Select(t => t.ID_NotificationType)
                .FirstOrDefaultAsync();
            if (id != 0)
                return id;

            return await _context.NotificationTypes
                .AsNoTracking()
                .Select(t => t.ID_NotificationType)
                .FirstOrDefaultAsync();
        }

        private static bool TryBuildCustomerRealtimeMessage(string? statusName, int orderNumber, out string title, out string body)
        {
            var s = (statusName ?? string.Empty).ToLowerInvariant();
            if (s.Contains("назнач"))
            {
                title = "Курьер едет к вам";
                body = $"Курьер назначен на заказ #{orderNumber} и направляется к точке забора.";
                return true;
            }
            if (s.Contains("в пути") || s.Contains("доставля"))
            {
                title = "Курьер доставляет заказ";
                body = $"Заказ #{orderNumber} уже в пути. Курьер движется к точке доставки.";
                return true;
            }
            if (s.Contains("доставлен") || s.Contains("delivered"))
            {
                title = "Заказ доставлен";
                body = $"Ваш заказ №{orderNumber} успешно доставлен.";
                return true;
            }
            if (s.Contains("отмен") || s.Contains("cancel"))
            {
                title = "Заказ отменён";
                body = $"Заказ №{orderNumber} отменён. Если оплата была списана, средства будут возвращены на карту.";
                return true;
            }
            title = string.Empty;
            body = string.Empty;
            return false;
        }

        public async Task<(bool ok, string? error)> ClientCompleteOrderPaymentAsync(int orderId, int userId)
        {
            var client = await _context.ClientProfiles.AsNoTracking()
                .FirstOrDefaultAsync(c => c.User_id == userId);
            if (client == null)
                return (false, "Профиль клиента не найден.");

            var order = await _context.Orders.FirstOrDefaultAsync(o =>
                o.ID_Order == orderId && o.Client_id == client.ID_ClientProfile);
            if (order == null)
                return (false, "Заказ не найден.");
            if (order.Is_paid)
                return (false, "Заказ уже оплачен.");

            var provider = _configuration["Billing:Provider"]?.Trim();
            if (string.IsNullOrWhiteSpace(provider))
                provider = "MockPay";

            if (!string.Equals(provider, "MockPay", StringComparison.OrdinalIgnoreCase))
                return (false, "Онлайн-оплата заказа картой пока доступна только в тестовом режиме (MockPay). Обратитесь в компанию доставки.");

            order.Is_paid = true;
            _context.OrderTimelineEvents.Add(new OrderTimelineEvent
            {
                Order_id = order.ID_Order,
                EventType = "PAYMENT_SUCCEEDED",
                Title = "Оплата",
                Message = "Заказ отмечен как оплаченный (тестовый режим MockPay)."
            });
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool ok, string? error)> DeleteMineAsync(int orderId, int userId)
        {
            var client = await _context.ClientProfiles.AsNoTracking()
                .FirstOrDefaultAsync(c => c.User_id == userId);
            if (client == null)
                return (false, "Профиль клиента не найден.");

            var order = await _context.Orders
                .Include(o => o.OrderStatus)
                .FirstOrDefaultAsync(o => o.ID_Order == orderId && o.Client_id == client.ID_ClientProfile);
            if (order == null)
                return (false, "Заказ не найден.");

            var statusName = order.OrderStatus?.Name ?? string.Empty;
            if (!CanClientDeleteOrder(order, statusName))
                return (false, "Отменить можно только новый заказ, пока курьер не назначен и доставка не началась.");

            var hasActiveAssignments = await _context.ShiftAssignments.AsNoTracking()
                .AnyAsync(a => a.Order_id == order.ID_Order &&
                               (a.Status == ShiftAssignmentStatus.Pending || a.Status == ShiftAssignmentStatus.InProgress));
            if (hasActiveAssignments)
                return (false, "Заказ уже находится в плане доставки и не может быть удалён клиентом.");

            var cancelledStatusId = await ResolveStatusIdByNameAsync("Отменён клиентом");
            if (cancelledStatusId == null || cancelledStatusId == 0)
                cancelledStatusId = await ResolveStatusIdByNameAsync("Отменён");
            if (cancelledStatusId == null || cancelledStatusId == 0)
                return (false, "Не настроен статус отмены заказа.");

            var oldStatusId = order.Status_id;
            order.Status_id = cancelledStatusId.Value;
            order.Courier_id = null;
            order.Plan_locked_shiftPlan_id = null;
            order.Plan_locked_at = null;
            order.Delay_reason = "Заказ отменён клиентом.";
            var (cancelWasPaid, cancelReason) = await ApplyOrderCancellationEffectsAsync(order, userId, "Заказ отменён клиентом.", oldStatusId);
            _context.OrderTimelineEvents.Add(new OrderTimelineEvent
            {
                Order_id = order.ID_Order,
                EventType = "CLIENT_CANCELLED",
                Title = "Заказ отменён клиентом",
                Message = "Клиент отменил заказ до передачи в доставку.",
                OldStatus_id = oldStatusId,
                NewStatus_id = cancelledStatusId,
                ActorUser_id = userId
            });
            await _context.SaveChangesAsync();
            await NotifyStaffOrderCancelledAsync(order, cancelWasPaid, cancelReason, userId);
            await PublishOrderEventAsync("order.cancelled_by_client", order, new { userId });
            await TryRebuildPlannerAsync(order.Company_id, "order.cancelled_by_client");
            return (true, null);
        }

        private async Task<(bool wasPaid, string reason)> ApplyOrderCancellationEffectsAsync(Order order, int? actorUserId, string reason, int? oldStatusId)
        {
            var wasPaid = order.Is_paid;
            if (wasPaid)
            {
                order.Is_paid = false;
                _context.OrderTimelineEvents.Add(new OrderTimelineEvent
                {
                    Order_id = order.ID_Order,
                    EventType = "PAYMENT_REFUNDED",
                    Title = "Возврат оплаты",
                    Message = "Оплата отменена, средства возвращены клиенту (тестовый режим MockPay).",
                    OldStatus_id = oldStatusId,
                    NewStatus_id = order.Status_id,
                    ActorUser_id = actorUserId
                });
            }

            await NotifyClientOrderCancelledAsync(order, wasPaid, reason);
            return (wasPaid, reason);
        }

        private async Task NotifyClientOrderCancelledAsync(Order order, bool wasPaid, string reason)
        {
            var typeId = await ResolveNotificationTypeIdAsync();
            if (typeId <= 0)
                return;

            var clientUserId = await _context.ClientProfiles.AsNoTracking()
                .Where(c => c.ID_ClientProfile == order.Client_id)
                .Select(c => c.User_id)
                .FirstOrDefaultAsync();

            var refundNote = wasPaid
                ? " Оплата возвращена на карту (тестовый MockPay)."
                : string.Empty;
            var clientTitle = "Заказ отменён";
            var clientMessage = $"Заказ №{order.Order_Number} отменён.{refundNote}";

            if (clientUserId > 0)
            {
                await _notificationService.SendAsync(
                    clientUserId,
                    typeId,
                    clientTitle,
                    clientMessage,
                    order.ID_Order,
                    priority: wasPaid ? (byte)1 : (byte)0,
                    isCritical: wasPaid);
            }
        }

        private async Task NotifyStaffOrderCancelledAsync(Order order, bool wasPaid, string reason, int? actorUserId)
        {
            var typeId = await ResolveNotificationTypeIdAsync();
            if (typeId <= 0)
                return;

            var refundNote = wasPaid
                ? " Оплата возвращена на карту (тестовый MockPay)."
                : string.Empty;
            var managerTitle = wasPaid
                ? $"Отменён оплаченный заказ №{order.Order_Number}"
                : $"Заказ №{order.Order_Number} отменён";
            var actorNote = actorUserId.HasValue && actorUserId.Value > 0
                ? $" Инициатор: пользователь #{actorUserId.Value}."
                : string.Empty;
            var managerMessage = $"{reason}{refundNote}{actorNote}";

            var staffUserIds = await GetStaffUserIdsForOrderAlertsAsync(order.Company_id);
            await _notificationService.SendManyAsync(
                staffUserIds,
                typeId,
                managerTitle,
                managerMessage,
                order.ID_Order,
                skipUserId: actorUserId,
                priority: wasPaid ? (byte)1 : (byte)0,
                isCritical: wasPaid,
                requiresAck: wasPaid);
        }

        private async Task<IReadOnlyList<int>> GetStaffUserIdsForOrderAlertsAsync(int companyId)
        {
            return await _context.Users.AsNoTracking()
                .Where(u => u.Company_id == companyId &&
                            (u.Role.Name == "Менеджер" || u.Role.Name == "Логист" || u.Role.Name == "Логистика" ||
                             u.Role.Name == "Админ" || u.Role.Name == "Администратор"))
                .Select(u => u.ID_User)
                .ToListAsync();
        }

        private async Task SendCourierPickupDeliveryAlertsAsync(
            Order order,
            int courierProfileId,
            bool isPickup,
            bool isDelivery,
            int? actorUserId)
        {
            var typeId = await ResolveNotificationTypeIdAsync();
            if (typeId <= 0)
                return;

            var orderName = string.IsNullOrWhiteSpace(order.Name)
                ? $"№{order.Order_Number}"
                : order.Name.Trim();

            var courier = await _context.CourierProfiles.AsNoTracking()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId);
            var courierName = courier?.User is null
                ? "Курьер"
                : $"{courier.User.FName} {courier.User.Name}".Trim();

            var clientUserId = await _context.ClientProfiles.AsNoTracking()
                .Where(c => c.ID_ClientProfile == order.Client_id)
                .Select(c => c.User_id)
                .FirstOrDefaultAsync();

            if (clientUserId > 0)
            {
                var clientTitle = isPickup ? "Заказ забран" : "Заказ доставлен";
                var clientBody = isPickup
                    ? $"Ваш заказ \"{orderName}\" забран"
                    : $"Ваш заказ \"{orderName}\" доставлен";
                await _notificationService.SendAsync(
                    clientUserId,
                    typeId,
                    clientTitle,
                    clientBody,
                    order.ID_Order,
                    priority: order.Priority,
                    isCritical: order.Priority >= 2,
                    requiresAck: order.Priority >= 2);
            }

            var staffIds = await GetStaffUserIdsForOrderAlertsAsync(order.Company_id);
            if (staffIds.Count == 0)
                return;

            var staffTitle = isPickup ? "Забор заказа" : "Доставка заказа";
            var staffBody = isPickup
                ? $"Курьер {courierName} забрал заказ \"{orderName}\""
                : $"Курьер {courierName} доставил заказ \"{orderName}\"";
            await _notificationService.SendManyAsync(
                staffIds,
                typeId,
                staffTitle,
                staffBody,
                order.ID_Order,
                skipUserId: actorUserId,
                priority: order.Priority >= 2 ? (byte)1 : (byte)0,
                isCritical: order.Priority >= 2);
        }

        private static (DateTime? fromUtc, DateTime? toUtc) GetDeliveryWindowUtc(Order order)
        {
            if (!order.Eta_at.HasValue && !order.Sla_due_at.HasValue)
                return (null, null);

            var to = order.Sla_due_at;
            if (order.Eta_at.HasValue)
                return (order.Eta_at, to);

            if (!to.HasValue)
                return (null, null);

            var tz = GetMoscowTimeZone();
            var dueLocal = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(to.Value, DateTimeKind.Utc), tz);
            var dayStartLocal = DateOnly.FromDateTime(dueLocal).ToDateTime(new TimeOnly(9, 0), DateTimeKind.Unspecified);
            var from = TimeZoneInfo.ConvertTimeToUtc(dayStartLocal, tz);
            return (from, to);
        }

        private static TimeZoneInfo GetMoscowTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            }
        }

        private static bool TryGetDeliveryWindowMismatch(Order order, out string kind, out string reasonRu)
        {
            kind = string.Empty;
            reasonRu = string.Empty;
            if (!order.Delivered_at.HasValue)
                return false;

            var (fromUtc, toUtc) = GetDeliveryWindowUtc(order);
            if (!fromUtc.HasValue && !toUtc.HasValue)
                return false;

            var tz = GetMoscowTimeZone();
            var deliveredLocal = TimeZoneInfo.ConvertTimeFromUtc(order.Delivered_at.Value, tz);
            var deliveredDay = DateOnly.FromDateTime(deliveredLocal);
            var plannedFromDay = fromUtc.HasValue
                ? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(fromUtc.Value, tz))
                : (DateOnly?)null;
            var plannedToDay = toUtc.HasValue
                ? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(toUtc.Value, tz))
                : (DateOnly?)null;
            var windowText = DeliveryWindowPolicy.FormatDisplayText(
                order.Priority, order.Sla_due_at, order.Eta_at, order.Created_at);
            var deliveredText = DeliveryWindowPolicy.FormatDayMonthRu(deliveredLocal);

            if (plannedFromDay.HasValue && deliveredDay < plannedFromDay)
            {
                kind = "early";
                reasonRu = $"Доставка выполнена {deliveredText} — раньше обещанного окна ({windowText}).";
                return true;
            }

            if (plannedToDay.HasValue && deliveredDay > plannedToDay)
            {
                kind = "late";
                reasonRu = $"Доставка выполнена {deliveredText} — позже обещанного окна ({windowText}).";
                return true;
            }

            if (fromUtc.HasValue && order.Delivered_at.Value < fromUtc.Value)
            {
                kind = "early";
                reasonRu = $"Доставка выполнена раньше начала окна ({windowText}).";
                return true;
            }

            if (toUtc.HasValue && order.Delivered_at.Value > toUtc.Value)
            {
                kind = "late";
                reasonRu = $"Доставка выполнена позже конца окна ({windowText}).";
                return true;
            }

            return false;
        }

        /// <summary>Фиксирует несоответствие окна доставки в заказе и таймлайне. Возвращает текст проблемы или null.</summary>
        private string? ApplyDeliveryWindowComplianceFields(Order order)
        {
            if (!TryGetDeliveryWindowMismatch(order, out var kind, out var reasonRu))
                return null;

            order.Delay_reason = reasonRu;
            if (kind == "late" && !order.Sla_breached_at.HasValue)
                order.Sla_breached_at = order.Delivered_at ?? DateTime.UtcNow;

            order.DeliveryWindowMismatch = true;
            order.DeliveryWindowMismatchKind = kind;

            _context.OrderTimelineEvents.Add(new OrderTimelineEvent
            {
                Order_id = order.ID_Order,
                EventType = "DELIVERY_WINDOW_MISMATCH",
                Title = kind == "early" ? "Доставка раньше срока" : "Доставка вне окна",
                Message = reasonRu,
                NewStatus_id = order.Status_id
            });

            return reasonRu;
        }

        private async Task PublishDeliveryWindowMismatchAlertsAsync(Order order, string reasonRu, int? actorUserId)
        {
            var typeId = await ResolveNotificationTypeIdAsync();
            if (typeId <= 0)
                return;

            var isLate = order.DeliveryWindowMismatchKind == "late";
            var staffTitle = isLate
                ? $"Заказ №{order.Order_Number}: доставка не в срок"
                : $"Заказ №{order.Order_Number}: доставка вне окна";
            var staffUserIds = await GetStaffUserIdsForOrderAlertsAsync(order.Company_id);
            await _notificationService.SendManyAsync(
                staffUserIds,
                typeId,
                staffTitle,
                reasonRu,
                order.ID_Order,
                skipUserId: actorUserId,
                priority: isLate ? (byte)1 : (byte)0,
                isCritical: isLate,
                requiresAck: isLate);

            var clientUserId = await _context.ClientProfiles.AsNoTracking()
                .Where(c => c.ID_ClientProfile == order.Client_id)
                .Select(c => c.User_id)
                .FirstOrDefaultAsync();
            if (clientUserId > 0)
            {
                var clientMessage = isLate
                    ? $"Заказ №{order.Order_Number} доставлен позже обещанного срока. Менеджер свяжется с вами или напишите в чат по заказу."
                    : $"Заказ №{order.Order_Number} доставлен раньше запланированного окна. Если возникли вопросы — напишите менеджеру в чате по заказу.";
                await _notificationService.SendAsync(
                    clientUserId,
                    typeId,
                    "Доставка вне обещанного окна",
                    clientMessage,
                    order.ID_Order,
                    priority: isLate ? (byte)1 : (byte)0,
                    isCritical: isLate);
            }

            await CreateDeliveryWindowMismatchTicketAsync(order, reasonRu, actorUserId, isLate);
        }

        private async Task CreateDeliveryWindowMismatchTicketAsync(Order order, string reasonRu, int? actorUserId, bool isLate)
        {
            var ticketExists = await _context.SupportTickets.AsNoTracking()
                .AnyAsync(t => t.Order_id == order.ID_Order &&
                               t.Category == SupportTicketCategory.Complaint &&
                               t.Status != SupportTicketStatus.Closed &&
                               t.Title.Contains("окно доставки"));
            if (ticketExists)
                return;

            var createdBy = actorUserId;
            if (!createdBy.HasValue || createdBy.Value <= 0)
            {
                createdBy = await _context.Users.AsNoTracking()
                    .Where(u => u.Company_id == order.Company_id &&
                                (u.Role.Name == "Менеджер" || u.Role.Name == "Админ" || u.Role.Name == "Администратор"))
                    .Select(u => u.ID_User)
                    .FirstOrDefaultAsync();
            }

            if (!createdBy.HasValue || createdBy.Value <= 0)
                return;

            var now = DateTime.UtcNow;
            var ticket = new SupportTicket
            {
                Company_id = order.Company_id,
                Order_id = order.ID_Order,
                ClientProfile_id = order.Client_id,
                Title = isLate
                    ? $"Нарушение окна доставки — заказ №{order.Order_Number}"
                    : $"Доставка вне окна — заказ №{order.Order_Number}",
                Description = $"{reasonRu} Требуется связаться с клиентом и зафиксировать решение (извинения, компенсация, повторная доставка).",
                Category = SupportTicketCategory.Complaint,
                Priority = isLate ? (byte)1 : (byte)0,
                Status = SupportTicketStatus.New,
                CreatedByUser_id = createdBy.Value,
                Created_at = now,
                Sla_due_at = now.AddHours(isLate ? 4 : 24),
                Delay_reason = reasonRu
            };
            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();
        }

        private async Task PublishOrderEventAsync(string eventType, Order order, object details)
        {
            var topic = _configuration["Kafka:OrderEventsTopic"] ?? "orders-events";
            var payload = new
            {
                eventType,
                occurredAtUtc = DateTime.UtcNow,
                companyId = order.Company_id,
                orderId = order.ID_Order,
                orderNumber = order.Order_Number,
                statusId = order.Status_id,
                courierId = order.Courier_id,
                priority = order.Priority,
                details
            };

            await _kafkaProducer.ProduceAsync(topic, payload, key: $"{order.Company_id}:{order.ID_Order}");
        }

        private async Task TryRebuildPlannerAsync(int companyId, string reason)
        {
            try
            {
                await _shiftPlanner.RebuildCompanyPlanAsync(companyId, reason);
            }
            catch
            {
                // Planner failures must not break order operations.
            }
        }

        private async Task TryEnsureCourierRouteAsync(int courierProfileId)
        {
            try
            {
                await _shiftPlanner.GetCourierPlanAsync(courierProfileId);
            }
            catch
            {
                // Route materialization must not break assignment.
            }
        }

        private async Task<string> GetCourierDisplayNameAsync(int courierProfileId)
        {
            var user = await _context.CourierProfiles.AsNoTracking()
                .Where(c => c.ID_CourierProfile == courierProfileId)
                .Select(c => new { c.User.FName, c.User.Name })
                .FirstOrDefaultAsync();
            return user == null
                ? "Курьер"
                : FormatCourierDisplayName(user.FName, user.Name);
        }

        private static string FormatCourierDisplayName(User? user)
        {
            if (user == null)
                return "Курьер";
            return FormatCourierDisplayName(user.FName, user.Name);
        }

        private static string FormatCourierDisplayName(string? firstName, string? surname)
        {
            var parts = new[] { surname, firstName }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .ToArray();
            return parts.Length == 0 ? "Курьер" : string.Join(' ', parts);
        }
    }
}


