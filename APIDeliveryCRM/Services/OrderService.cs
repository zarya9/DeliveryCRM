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

        public OrderService(
            ContextDB context,
            ICommunicationTemplateService templateService,
            INotificationService notificationService,
            IHubContext<ChatHub> hubContext,
            IKafkaProducer kafkaProducer,
            IFuelPriceService fuelPriceService,
            IConfiguration configuration,
            IShiftPlannerService shiftPlanner)
        {
            _context = context;
            _templateService = templateService;
            _notificationService = notificationService;
            _hubContext = hubContext;
            _kafkaProducer = kafkaProducer;
            _fuelPriceService = fuelPriceService;
            _configuration = configuration;
            _shiftPlanner = shiftPlanner;
        }

        public async Task<Order> GetByIdAsync(int id)
        {
            return await _context.Orders
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
            return await _context.Orders
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
        }

        public async Task<IReadOnlyList<Order>> GetByCourierAsync(int courierProfileId)
        {
            return await _context.Orders
                .Where(o => o.Courier_id == courierProfileId)
                .Include(o => o.OrderStatus)
                .Include(o => o.ClientProfile)
                .ToListAsync();
        }

        public async Task<Order> CreateAsync(CreateOrderRequest request)
        {
            var client = await _context.ClientProfiles.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ID_ClientProfile == request.Client_id);
            if (client == null)
                throw new InvalidOperationException("Клиент не найден.");

            var company = await _context.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ID_Company == client.Company_id);
            if (company == null)
                throw new InvalidOperationException("Компания клиента не найдена.");

            if (company.SubscriptionExpiresAt != default && company.SubscriptionExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Подписка компании истекла. Продлите тариф для создания заказов.");

            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthlyOrders = await _context.Orders
                .AsNoTracking()
                .CountAsync(o => o.Company_id == client.Company_id && o.Created_at >= monthStart);
            if (monthlyOrders >= company.MaxOrdersPerMonth)
                throw new InvalidOperationException("Достигнут лимит заказов по тарифу за текущий месяц.");

            if (!Enum.IsDefined(typeof(DeliveryRouteKind), request.DeliveryRouteKind))
                throw new InvalidOperationException("Некорректный тип маршрута доставки.");

            var routeKind = (DeliveryRouteKind)request.DeliveryRouteKind;

            var pickup = await _context.Addresses.FirstOrDefaultAsync(a =>
                a.ID_Address == request.PickupAddress_id && a.Company_id == client.Company_id);
            var delivery = await _context.Addresses.FirstOrDefaultAsync(a =>
                a.ID_Address == request.DeliveryAddress_id && a.Company_id == client.Company_id);
            if (pickup == null || delivery == null)
                throw new InvalidOperationException("Адреса забора или доставки не найдены или не принадлежат компании клиента.");

            LogisticsHub? originHub = null;
            LogisticsHub? destHub = null;
            if (routeKind == DeliveryRouteKind.ViaHub)
            {
                if (!request.OriginHub_id.HasValue || !request.DestinationHub_id.HasValue)
                    throw new InvalidOperationException("Для доставки через хабы укажите склад отправления и склад назначения.");

                originHub = await _context.LogisticsHubs
                    .Include(h => h.Address)
                    .FirstOrDefaultAsync(h =>
                        h.ID_LogisticsHub == request.OriginHub_id && h.Company_id == client.Company_id);
                destHub = await _context.LogisticsHubs
                    .Include(h => h.Address)
                    .FirstOrDefaultAsync(h =>
                        h.ID_LogisticsHub == request.DestinationHub_id && h.Company_id == client.Company_id);
                if (originHub == null || destHub == null)
                    throw new InvalidOperationException("Один из складов не найден или принадлежит другой компании.");
            }
            var stops = OrderRoutePlanner.BuildStops(routeKind, pickup, delivery, originHub, destHub);
            var distanceKm = EstimateRouteDistanceKm(routeKind, pickup, delivery, originHub, destHub);
            var fuelCostRub = await EstimateFuelCostRubAsync(
                companyId: client.Company_id,
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
            var (windowFromUtc, windowToUtc, etaUtc) = EstimateDeliveryWindowUtc(
                nowUtc: DateTime.UtcNow,
                priority: request.Priority,
                hasCourierAssigned: request.Courier_id.HasValue,
                routeKind: routeKind,
                distanceKm: distanceKm);

            // EF Core не переводит Select(...).DefaultIfEmpty(0).MaxAsync() в SQL (Npgsql).
            var maxOrderNumber = await _context.Orders.MaxAsync(o => (int?)o.Order_Number) ?? 0;

            var order = new Order
            {
                Name = request.Name,
                Description = request.Description,
                Order_Number = maxOrderNumber + 1,
                Company_id = client.Company_id,
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
                Sla_due_at = request.RequestedDeliveryAtUtc?.ToUniversalTime() ?? windowToUtc,
                Eta_at = etaUtc
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
                Message = $"Создан новый заказ. Ориентировочная доставка: {FormatDeliveryWindowRu(windowFromUtc, windowToUtc)}. Предварительная стоимость: {estimatedCost:0.##} ₽."
            });
            await _context.SaveChangesAsync();
            await PublishOrderEventAsync("order.created", order, new
            {
                routeKind = order.DeliveryRouteKind.ToString(),
                priority = order.Priority,
                etaAt = order.Eta_at,
                deliveryWindowFromUtc = windowFromUtc,
                deliveryWindowToUtc = windowToUtc,
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

        public async Task<bool> ChangeStatusAsync(int orderId, int statusId)
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
            order.Status_id = statusId;
            ApplyMilestoneTimestamps(order, statusId);
            UpdateSlaFlags(order);
            order.Eta_at = EstimateEtaUtc(DateTime.UtcNow, order.Priority, order.Courier_id.HasValue);
            var isSlaRisk = IsSlaRisk(order);

            _context.OrderTimelineEvents.Add(new OrderTimelineEvent
            {
                Order_id = order.ID_Order,
                EventType = "STATUS_CHANGED",
                Title = "Изменение статуса",
                Message = $"Статус заказа изменен: {oldStatusId} -> {statusId}",
                OldStatus_id = oldStatusId,
                NewStatus_id = statusId
            });

            var activeAssignment = await _context.ShiftAssignments
                .Where(a => a.Order_id == order.ID_Order && (a.Status == ShiftAssignmentStatus.Pending || a.Status == ShiftAssignmentStatus.InProgress))
                .OrderByDescending(a => a.Assignment_sequence)
                .FirstOrDefaultAsync();
            ApplyHandoffByStatusEvent(order, statusId, activeAssignment);

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
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId && c.Company_id == order.Company_id);
            if (courier == null)
                return false;
            if (!courier.Is_online)
                return false;

            var allowed = await IsCourierAllowedByZonesAsync(order, courierProfileId);
            if (!allowed)
                return false;

            var oldCourierId = order.Courier_id;
            order.Courier_id = courierProfileId;
            order.Eta_at = EstimateEtaUtc(DateTime.UtcNow, order.Priority, hasCourierAssigned: true);

            _context.OrderTimelineEvents.Add(new OrderTimelineEvent
            {
                Order_id = order.ID_Order,
                EventType = "COURIER_ASSIGNED",
                Title = "Назначен курьер",
                Message = $"Курьер {courierProfileId} назначен на заказ.",
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
            await TryRebuildPlannerAsync(order.Company_id, "order.assigned");
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
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId && c.Company_id == order.Company_id);
            if (courier == null)
                return false;
            if (!courier.Is_online)
                return false;

            var allowed = await IsCourierAllowedByZonesAsync(order, courierProfileId);
            if (!allowed)
                return false;

            var oldCourierId = order.Courier_id;
            order.Courier_id = courierProfileId;
            order.Eta_at = EstimateEtaUtc(DateTime.UtcNow, order.Priority, hasCourierAssigned: true);

            _context.OrderTimelineEvents.Add(new OrderTimelineEvent
            {
                Order_id = order.ID_Order,
                EventType = "MANUAL_OVERRIDE",
                Title = "Ручное переназначение",
                Message = string.IsNullOrWhiteSpace(reason) ? "Курьер изменен логистом вручную." : reason,
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
            await TryRebuildPlannerAsync(order.Company_id, "order.manual_override");
            return true;
        }

        public async Task<OrderDispatchDto?> AutoDispatchAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.PickupAddress)
                .FirstOrDefaultAsync(o => o.ID_Order == orderId);
            if (order == null)
                return null;

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

            var filteredCandidates = new List<(int CourierId, decimal Lat, decimal Lon, int ActiveOrders)>();
            foreach (var c in candidates)
            {
                if (await IsCourierAllowedByZonesAsync(order, c.ID_CourierProfile))
                    filteredCandidates.Add((c.ID_CourierProfile, c.Current_lat, c.Current_lon, c.ActiveOrders));
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
                    var urgentBoost = order.Priority == 2 ? -3.0 : order.Priority == 1 ? -1.5 : 0.0;
                    var total = distanceScore + loadScore + urgentBoost;

                    return new
                    {
                        ID_CourierProfile = c.CourierId,
                        c.ActiveOrders,
                        DistanceKm = distance,
                        Score = total
                    };
                })
                .OrderBy(x => x.Score)
                .ToList();

            var winner = scored.First();
            var oldCourierId = order.Courier_id;
            order.Courier_id = winner.ID_CourierProfile;
            order.Eta_at = EstimateEtaUtc(DateTime.UtcNow, order.Priority, hasCourierAssigned: true);

            var isSlaRisk = IsSlaRisk(order);
            _context.OrderTimelineEvents.Add(new OrderTimelineEvent
            {
                Order_id = order.ID_Order,
                EventType = "AUTO_DISPATCH",
                Title = "Авто-диспетчеризация",
                Message = $"Автоназначение курьера {winner.ID_CourierProfile} (дистанция: {winner.DistanceKm?.ToString("0.0") ?? "n/a"} км, активных заказов: {winner.ActiveOrders}).",
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
                DecisionReason = "Минимальный совокупный score: дистанция + текущая загрузка + приоритет SLA."
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
            var windowFrom = order.Eta_at?.AddHours(-Math.Max(2, Math.Ceiling(((order.Sla_due_at ?? order.Eta_at ?? DateTime.UtcNow) - (order.Eta_at ?? DateTime.UtcNow)).TotalHours)));
            var windowTo = order.Sla_due_at;
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
                DeliveryWindowText = FormatDeliveryWindowRu(windowFrom, windowTo)
            };
        }

        private static (DateTime fromUtc, DateTime toUtc, DateTime etaUtc) EstimateDeliveryWindowUtc(
            DateTime nowUtc,
            byte priority,
            bool hasCourierAssigned,
            DeliveryRouteKind routeKind,
            decimal distanceKm)
        {
            var safeDistance = Math.Max(1m, distanceKm);
            var speedKmh = routeKind switch
            {
                DeliveryRouteKind.DirectIntercity => 62m,
                DeliveryRouteKind.ViaHub => 48m,
                _ => 32m
            };
            var routeHandlingHours = routeKind switch
            {
                DeliveryRouteKind.ViaHub => 8m,
                DeliveryRouteKind.DirectIntercity => 5m,
                _ => 2m
            };
            var travelHours = safeDistance / speedKmh + routeHandlingHours;
            var priorityFactor = priority switch
            {
                2 => 0.70m,
                1 => 0.82m,
                _ => 1.00m
            };
            travelHours *= priorityFactor;
            if (!hasCourierAssigned)
                travelHours += 2.0m;

            var center = nowUtc.AddHours((double)travelHours);
            var halfWindowHours = Math.Max(2.0, Math.Ceiling((double)travelHours * 0.25));
            var from = center.AddHours(-halfWindowHours);
            var to = center.AddHours(halfWindowHours);
            return (from, to, center);
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

        private static string FormatDeliveryWindowRu(DateTime? fromUtc, DateTime? toUtc)
        {
            if (!fromUtc.HasValue && !toUtc.HasValue)
                return "дата уточняется";
            if (!fromUtc.HasValue)
                return $"до {FormatDayMonthRu(toUtc!.Value)}";
            if (!toUtc.HasValue)
                return $"с {FormatDayMonthRu(fromUtc.Value)}";

            var from = fromUtc.Value;
            var to = toUtc.Value;
            if (from.Date == to.Date)
                return FormatDayMonthRu(from);

            if (from.Month == to.Month && from.Year == to.Year)
                return $"с {from:dd} по {to:dd} {GetMonthRu(to.Month)}";

            return $"с {FormatDayMonthRu(from)} по {FormatDayMonthRu(to)}";
        }

        private static string FormatDayMonthRu(DateTime dt) => $"{dt:dd} {GetMonthRu(dt.Month)}";

        private static string GetMonthRu(int month) => month switch
        {
            1 => "января",
            2 => "февраля",
            3 => "марта",
            4 => "апреля",
            5 => "мая",
            6 => "июня",
            7 => "июля",
            8 => "августа",
            9 => "сентября",
            10 => "октября",
            11 => "ноября",
            12 => "декабря",
            _ => string.Empty
        };

        private static DateTime EstimateEtaUtc(DateTime nowUtc, byte priority, bool hasCourierAssigned)
        {
            var baseMinutes = priority switch
            {
                2 => 45,
                1 => 90,
                _ => 150
            };

            if (!hasCourierAssigned)
                baseMinutes += 30;

            return nowUtc.AddMinutes(baseMinutes);
        }

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

        private static void ApplyMilestoneTimestamps(Order order, int statusId)
        {
            // Базовое соответствие для быстрых интеграций со статусами 3/4/5.
            if (statusId == 3 && !order.Pickup_started_at.HasValue)
                order.Pickup_started_at = DateTime.UtcNow;
            if (statusId == 4 && !order.In_transit_at.HasValue)
                order.In_transit_at = DateTime.UtcNow;
            if (statusId == 5 && !order.Delivered_at.HasValue)
                order.Delivered_at = DateTime.UtcNow;
        }

        private void ApplyHandoffByStatusEvent(Order order, int statusId, ShiftAssignment? activeAssignment)
        {
            if (order.DeliveryRouteKind != DeliveryRouteKind.ViaHub || activeAssignment == null)
                return;

            var stage = activeAssignment.Stage;
            var shouldAdvanceLeg = statusId >= 4; // in-transit/reached transfer point
            var isFinalDelivery = statusId >= 5;
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
            if (!lat.HasValue || !lon.HasValue)
                return false;

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

            if (zones.Count == 0)
                return false;

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
    }
}


