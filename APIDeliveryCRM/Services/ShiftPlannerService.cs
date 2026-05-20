using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using APIDeliveryCRM.Responses;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services;

public class ShiftPlannerService : IShiftPlannerService
{
    private const decimal DefaultVolumeM3 = 2.5m;
    private const decimal DefaultWeightKg = 450m;
    private const decimal AvgSpeedKmH = 28m;
    private const decimal StopServiceMinutes = 8m;
    private const decimal NoVehiclePenalty = 6.0m;
    private const decimal CriticalLoadPenaltyWeight = 8.0m;
    private const decimal SlaLateMinutePenalty = 0.03m;

    /// <summary>Приоритет заказа: 3 — критически срочный (ускоренная последовательность точек в городской связке).</summary>
    private const byte OrderPriorityCriticallyUrgent = 3;

    /// <summary>Макс. прямое расстояние забрать→вручить (км), при котором считаем «один город / соседняя агломерация» для ускорения критически срочных.</summary>
    private const decimal CriticalUrbanClusterMaxDirectKm = 85m;

    /// <summary>Виртуальные «минус км» к оценке следующей точки — чем больше, тем раньше берём забор критически срочного городского заказа.</summary>
    private const decimal CriticalUrbanPickupScoreBiasKm = 48m;

    /// <summary>После забора критически срочного городского заказа сильно тянем доставку вперёд по маршруту.</summary>
    private const decimal CriticalUrbanDropScoreBiasKm = 165m;

    /// <summary>Заборы двух заказов ближе этого расстояния (км) — кластер заборов.</summary>
    private const decimal PickupClusterMaxKm = 5.5m;

    /// <summary>На каждый ещё не сделанный забор в том же кластере усиливаем привлекательность этого забора (км-экв. к score), без штрафов на доставку.</summary>
    private const decimal PickupClusterPickupAffinityKmPerNeighbor = 38m;

    private readonly ContextDB _context;
    private readonly IFuelPriceService _fuelPriceService;

    public ShiftPlannerService(ContextDB context, IFuelPriceService fuelPriceService)
    {
        _context = context;
        _fuelPriceService = fuelPriceService;
    }

    public async Task<CompanyPlannerResultDto> RebuildCompanyPlanAsync(int companyId, string reason, CancellationToken cancellationToken = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var shifts = await _context.CourierShifts
            .Include(s => s.CourierProfile).ThenInclude(c => c.User)
            .Where(s => s.Company_id == companyId && s.TimeEnd == null)
            .OrderBy(s => s.TimeStart)
            .ToListAsync(cancellationToken);

        var courierIds = shifts.Select(s => s.Courier_id).Distinct().ToArray();
        var vehicles = await _context.Vehicles
            .Include(v => v.VehicleModel)
            .Where(v => v.Company_id == companyId && v.CurrentCourier_id.HasValue && courierIds.Contains(v.CurrentCourier_id.Value))
            .ToListAsync(cancellationToken);
        var vehicleByCourier = vehicles
            .GroupBy(v => v.CurrentCourier_id!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.ID_Vehicle).First());

        var stalePlans = await _context.ShiftPlans
            .Where(p => p.Company_id == companyId && (p.Status == ShiftPlanStatus.Draft || p.Status == ShiftPlanStatus.Active))
            .ToListAsync(cancellationToken);
        var stalePlanIds = stalePlans.Select(x => x.ID_ShiftPlan).ToArray();
        if (stalePlanIds.Length > 0)
        {
            foreach (var p in stalePlans)
                p.Status = ShiftPlanStatus.Replanned;

            var oldAssignments = await _context.ShiftAssignments
                .Where(a => a.ShiftPlan_id.HasValue && stalePlanIds.Contains(a.ShiftPlan_id.Value) &&
                            (a.Status == ShiftAssignmentStatus.Pending || a.Status == ShiftAssignmentStatus.InProgress))
                .ToListAsync(cancellationToken);
            foreach (var a in oldAssignments)
                a.Status = ShiftAssignmentStatus.Reassigned;

            var lockedOrders = await _context.Orders
                .Where(o => o.Plan_locked_shiftPlan_id.HasValue && stalePlanIds.Contains(o.Plan_locked_shiftPlan_id.Value))
                .ToListAsync(cancellationToken);
            foreach (var o in lockedOrders)
            {
                o.Plan_locked_shiftPlan_id = null;
                o.Plan_locked_at = null;
            }
        }

        var orders = await _context.Orders
            .Include(o => o.PickupAddress)
            .Include(o => o.DeliveryAddress)
            .Include(o => o.OriginHub).ThenInclude(h => h!.Address)
            .Include(o => o.DestinationHub).ThenInclude(h => h!.Address)
            .Include(o => o.RouteStops).ThenInclude(s => s.Address)
            .Where(o => o.Company_id == companyId && o.Delivered_at == null)
            .ToListAsync(cancellationToken);

        var candidates = orders
            .Where(o => !o.Plan_locked_shiftPlan_id.HasValue)
            .OrderByDescending(o => o.Priority)
            .ThenBy(o => o.Sla_due_at ?? DateTime.MaxValue)
            .ThenBy(o => o.Created_at)
            .ToList();

        var routeStates = shifts.Select(s =>
        {
            vehicleByCourier.TryGetValue(s.Courier_id, out var v);
            return new CourierRouteState
            {
                Shift = s,
                Vehicle = v,
                MaxWeightKg = v?.Max_cargo_weight > 0 ? v.Max_cargo_weight : DefaultWeightKg,
                MaxVolumeM3 = v?.Cargo_volume > 0 ? v.Cargo_volume : DefaultVolumeM3,
                CurrentLat = s.CourierProfile.Current_lat != 0 ? (double?)s.CourierProfile.Current_lat : null,
                CurrentLon = s.CourierProfile.Current_lon != 0 ? (double?)s.CourierProfile.Current_lon : null,
                IsCourierOnline = s.CourierProfile.Is_online,
                HasOperationalVehicle = IsVehicleOperational(v),
                VehicleHealthPenalty = ComputeVehicleHealthPenalty(v),
                CursorAtUtc = now
            };
        }).ToList();

        var unplanned = new List<UnplannedOrderDto>();
        foreach (var order in candidates)
        {
            if (routeStates.Count == 0)
            {
                unplanned.Add(ToUnplanned(order, "Нет активных смен"));
                continue;
            }

            var placed = TryPlanOrder(order, routeStates, now);
            if (!placed)
                unplanned.Add(ToUnplanned(order, "Нет доступного курьера/ТС или не хватает вместимости/координат"));
        }

        var plans = new List<ShiftPlan>();
        foreach (var state in routeStates.Where(x => x.Stops.Count > 0))
        {
            var nextVersion = (await _context.ShiftPlans
                .Where(p => p.Shift_id == state.Shift.ID_Shift)
                .Select(p => (int?)p.Version)
                .MaxAsync(cancellationToken) ?? 0) + 1;

            var plan = new ShiftPlan
            {
                Company_id = companyId,
                Shift_id = state.Shift.ID_Shift,
                Courier_id = state.Shift.Courier_id,
                Vehicle_id = state.Vehicle?.ID_Vehicle,
                Status = ShiftPlanStatus.Active,
                Created_at = now,
                Activated_at = now,
                Planned_start_utc = now,
                Planned_end_utc = state.Stops.Max(s => s.PlannedEndUtc),
                Total_distance_km = Math.Round(state.Stops.Sum(s => s.DistanceKm), 3),
                Estimated_duration_minutes = Math.Round(state.Stops.Sum(s => s.DurationMinutes), 2),
                Peak_weight_kg = Math.Round(state.PeakWeightKg, 3),
                Peak_volume_m3 = Math.Round(state.PeakVolumeM3, 4),
                Version = nextVersion,
                Last_recompute_reason = string.IsNullOrWhiteSpace(reason) ? "manual" : reason.Trim()
            };
            _context.ShiftPlans.Add(plan);
            await _context.SaveChangesAsync(cancellationToken);

            var seq = 1;
            foreach (var stop in state.Stops.OrderBy(s => s.Sequence))
            {
                var assignment = new ShiftAssignment
                {
                    Company_id = companyId,
                    Shift_id = state.Shift.ID_Shift,
                    ShiftPlan_id = plan.ID_ShiftPlan,
                    Order_id = stop.Order.ID_Order,
                    Assignment_sequence = seq++,
                    OrderRouteStop_id = stop.OrderRouteStopId,
                    Stage = stop.Stage,
                    Status = ShiftAssignmentStatus.Pending,
                    Planned_start_utc = stop.PlannedStartUtc,
                    Planned_end_utc = stop.PlannedEndUtc,
                    Planned_distance_km = Math.Round(stop.DistanceKm, 3),
                    Notes = stop.Notes
                };
                _context.ShiftAssignments.Add(assignment);

                stop.Order.Plan_locked_shiftPlan_id = plan.ID_ShiftPlan;
                stop.Order.Plan_locked_at = now;
                if (!stop.Order.Courier_id.HasValue)
                    stop.Order.Courier_id = state.Shift.Courier_id;
                stop.Order.HandoffStage = stop.Stage switch
                {
                    ShiftAssignmentStage.PickupToHub => OrderHandoffStage.AwaitingHubDropOff,
                    ShiftAssignmentStage.HubToHub => OrderHandoffStage.AtHub,
                    ShiftAssignmentStage.HubToRecipient => OrderHandoffStage.LastMileInProgress,
                    ShiftAssignmentStage.LocalUrban => OrderHandoffStage.None,
                    _ => stop.Order.HandoffStage
                };

                _context.OrderTimelineEvents.Add(new OrderTimelineEvent
                {
                    Order_id = stop.Order.ID_Order,
                    EventType = "PLAN_LOCK",
                    Title = "Заказ включен в маршрут",
                    Message = $"Заказ включен в план смены #{state.Shift.ID_Shift}. Этап: {stop.Stage}. Причина: {plan.Last_recompute_reason}"
                });
            }

            plans.Add(plan);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        var result = await GetCompanyPlanAsync(companyId, cancellationToken);
        result.Reason = string.IsNullOrWhiteSpace(reason) ? "manual" : reason.Trim();
        result.ConsideredOrders = candidates.Count;
        result.Unplanned = unplanned;
        return result;
    }

    public async Task<CompanyPlannerResultDto> GetCompanyPlanAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var plans = await _context.ShiftPlans
            .Include(p => p.CourierProfile).ThenInclude(c => c.User)
            .Include(p => p.Vehicle)
            .Include(p => p.Assignments).ThenInclude(a => a.Order)
            .Include(p => p.Assignments).ThenInclude(a => a.OrderRouteStop)!.ThenInclude(s => s!.Address)
            .Where(p => p.Company_id == companyId && (p.Status == ShiftPlanStatus.Active || p.Status == ShiftPlanStatus.Draft))
            .OrderBy(p => p.Courier_id)
            .ThenByDescending(p => p.Created_at)
            .ToListAsync(cancellationToken);

        var activeShifts = await _context.CourierShifts.CountAsync(s => s.Company_id == companyId && s.TimeEnd == null, cancellationToken);
        var onlineCouriers = await _context.CourierProfiles.CountAsync(c => c.Company_id == companyId && c.Is_online, cancellationToken);

        return new CompanyPlannerResultDto
        {
            CompanyId = companyId,
            ActiveShifts = activeShifts,
            OnlineCouriers = onlineCouriers,
            Plans = plans.Select(MapPlan).ToList()
        };
    }

    public async Task<ShiftPlanSummaryDto?> GetActivePlanForCourierAsync(int courierProfileId, CancellationToken cancellationToken = default)
    {
        var plan = await _context.ShiftPlans
            .Include(p => p.CourierProfile).ThenInclude(c => c.User)
            .Include(p => p.Vehicle)
            .Include(p => p.Assignments).ThenInclude(a => a.Order)
            .Include(p => p.Assignments).ThenInclude(a => a.OrderRouteStop)!.ThenInclude(s => s!.Address)
            .Where(p => p.Courier_id == courierProfileId && (p.Status == ShiftPlanStatus.Active || p.Status == ShiftPlanStatus.Draft))
            .OrderByDescending(p => p.Created_at)
            .FirstOrDefaultAsync(cancellationToken);

        return plan == null ? null : MapPlan(plan);
    }

    public async Task<ShiftPlanSummaryDto?> GetCourierPlanAsync(int courierProfileId, CancellationToken cancellationToken = default)
    {
        var active = await GetActivePlanForCourierAsync(courierProfileId, cancellationToken);
        if (active != null && active.Stops.Count > 0)
            return active;

        return await EnsureCourierPlanFromAssignedOrdersAsync(courierProfileId, cancellationToken);
    }

    public async Task<ShiftPlanSummaryDto?> ApplyCourierRouteAsync(
        int companyId,
        int courierProfileId,
        IReadOnlyList<ApplyCourierRouteStopRequest> stops,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (stops.Count == 0)
            return null;

        var courier = await _context.CourierProfiles
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId && c.Company_id == companyId, cancellationToken);
        if (courier == null)
            return null;

        var orderedStops = stops
            .Where(s => s.Latitude.HasValue && s.Longitude.HasValue)
            .OrderBy(s => s.Sequence)
            .ToList();
        if (orderedStops.Count == 0)
            return null;

        var orderIds = orderedStops
            .Where(s => s.OrderId is > 0)
            .Select(s => s.OrderId!.Value)
            .Distinct()
            .ToList();

        var orders = orderIds.Count == 0
            ? new List<Order>()
            : await _context.Orders
                .Include(o => o.RouteStops).ThenInclude(rs => rs.Address)
                .Include(o => o.PickupAddress)
                .Include(o => o.DeliveryAddress)
                .Where(o => o.Company_id == companyId && orderIds.Contains(o.ID_Order))
                .ToListAsync(cancellationToken);

        foreach (var order in orders)
            order.Courier_id = courierProfileId;

        var shift = await EnsureActiveShiftForCourierAsync(courierProfileId, companyId, cancellationToken);
        if (shift == null)
            return BuildSyntheticPlanFromOrderedStops(courier, orderedStops, orders);

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var stalePlans = await _context.ShiftPlans
            .Where(p => p.Courier_id == courierProfileId && (p.Status == ShiftPlanStatus.Active || p.Status == ShiftPlanStatus.Draft))
            .ToListAsync(cancellationToken);
        foreach (var stale in stalePlans)
            stale.Status = ShiftPlanStatus.Replanned;

        var staleAssignments = await _context.ShiftAssignments
            .Where(a => a.Shift_id == shift.ID_Shift &&
                        (a.Status == ShiftAssignmentStatus.Pending || a.Status == ShiftAssignmentStatus.InProgress))
            .ToListAsync(cancellationToken);
        foreach (var a in staleAssignments)
            a.Status = ShiftAssignmentStatus.Reassigned;

        var vehicle = await _context.Vehicles
            .Where(v => v.Company_id == companyId && v.CurrentCourier_id == courierProfileId)
            .OrderByDescending(v => v.ID_Vehicle)
            .FirstOrDefaultAsync(cancellationToken);

        var nextVersion = (await _context.ShiftPlans
            .Where(p => p.Shift_id == shift.ID_Shift)
            .Select(p => (int?)p.Version)
            .MaxAsync(cancellationToken) ?? 0) + 1;

        var plan = new ShiftPlan
        {
            Company_id = companyId,
            Shift_id = shift.ID_Shift,
            Courier_id = courierProfileId,
            Vehicle_id = vehicle?.ID_Vehicle,
            Status = ShiftPlanStatus.Active,
            Created_at = now,
            Activated_at = now,
            Planned_start_utc = now,
            Version = nextVersion,
            Last_recompute_reason = string.IsNullOrWhiteSpace(reason) ? "logistician.route_map" : reason.Trim()
        };
        _context.ShiftPlans.Add(plan);
        await _context.SaveChangesAsync(cancellationToken);

        var seq = 1;
        var newAssignments = new List<ShiftAssignment>();
        foreach (var stop in orderedStops)
        {
            if (stop.OrderId is not > 0)
                continue;

            var order = orders.FirstOrDefault(o => o.ID_Order == stop.OrderId!.Value);
            if (order == null)
                continue;

            var routeStopId = stop.OrderRouteStopId;
            if (!routeStopId.HasValue)
                routeStopId = ResolveRouteStopIdFromTitle(order, stop.Title);

            var stage = ShiftAssignmentStage.LocalUrban;
            if (routeStopId.HasValue)
            {
                var routeStop = order.RouteStops.FirstOrDefault(rs => rs.ID_OrderRouteStop == routeStopId.Value);
                if (routeStop != null)
                    stage = ResolveStageForStop(order, routeStop);
            }

            var assignment = new ShiftAssignment
            {
                Company_id = companyId,
                Shift_id = shift.ID_Shift,
                ShiftPlan_id = plan.ID_ShiftPlan,
                Order_id = order.ID_Order,
                Assignment_sequence = seq++,
                OrderRouteStop_id = routeStopId,
                Stage = stage,
                Status = ShiftAssignmentStatus.Pending,
                Planned_start_utc = now,
                Notes = stop.Title
            };
            _context.ShiftAssignments.Add(assignment);
            newAssignments.Add(assignment);

            order.Plan_locked_shiftPlan_id = plan.ID_ShiftPlan;
            order.Plan_locked_at = now;
        }

        ApplySegmentDistancesToAssignments(newAssignments, orderedStops);
        plan.Total_distance_km = Math.Round(newAssignments.Sum(a => a.Planned_distance_km), 3);
        plan.Estimated_duration_minutes = Math.Round(newAssignments.Count * StopServiceMinutes + plan.Total_distance_km / AvgSpeedKmH * 60m, 1);

        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return await GetActivePlanForCourierAsync(courierProfileId, cancellationToken);
    }

    public async Task<IReadOnlyList<CourierRouteMapWaypointDto>> GetCourierRouteWaypointsAsync(
        int courierProfileId,
        CancellationToken cancellationToken = default)
    {
        var plan = await _context.ShiftPlans
            .Include(p => p.Assignments).ThenInclude(a => a.OrderRouteStop)!.ThenInclude(s => s!.Address)
            .Include(p => p.Assignments).ThenInclude(a => a.Order).ThenInclude(o => o!.PickupAddress)
            .Include(p => p.Assignments).ThenInclude(a => a.Order).ThenInclude(o => o!.DeliveryAddress)
            .Where(p => p.Courier_id == courierProfileId && (p.Status == ShiftPlanStatus.Active || p.Status == ShiftPlanStatus.Draft))
            .OrderByDescending(p => p.Created_at)
            .FirstOrDefaultAsync(cancellationToken);

        if (plan == null)
            return Array.Empty<CourierRouteMapWaypointDto>();

        var list = new List<CourierRouteMapWaypointDto>();
        foreach (var a in plan.Assignments
                     .Where(x => x.Status is ShiftAssignmentStatus.Pending or ShiftAssignmentStatus.InProgress)
                     .OrderBy(x => x.Assignment_sequence))
        {
            var (lat, lon, title) = ResolveAssignmentMapPoint(a);
            if (!lat.HasValue || !lon.HasValue)
                continue;

            list.Add(new CourierRouteMapWaypointDto
            {
                Sequence = a.Assignment_sequence,
                OrderId = a.Order_id > 0 ? a.Order_id : null,
                AssignmentId = a.ID_ShiftAssignment,
                Title = title ?? a.Notes,
                Lat = lat.Value,
                Lon = lon.Value
            });
        }

        return list;
    }

    private async Task<CourierShift?> EnsureActiveShiftForCourierAsync(
        int courierProfileId,
        int companyId,
        CancellationToken cancellationToken)
    {
        var shift = await _context.CourierShifts
            .Where(s => s.Courier_id == courierProfileId && s.Company_id == companyId && s.TimeEnd == null)
            .OrderByDescending(s => s.TimeStart)
            .FirstOrDefaultAsync(cancellationToken);
        if (shift != null)
            return shift;

        var statusId = await _context.ShiftStatuses.AsNoTracking()
            .Where(s => s.Name == "Active" || s.Name == "Активна")
            .Select(s => (int?)s.ID_ShiftStatus)
            .FirstOrDefaultAsync(cancellationToken) ?? 1;

        shift = new CourierShift
        {
            Company_id = companyId,
            Courier_id = courierProfileId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            TimeStart = DateTime.UtcNow,
            ShiftStatus_id = statusId
        };
        _context.CourierShifts.Add(shift);
        await _context.SaveChangesAsync(cancellationToken);
        return shift;
    }

    private static int? ResolveRouteStopIdFromTitle(Order order, string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var t = title.ToLowerInvariant();
        var stops = order.RouteStops.OrderBy(s => s.SortOrder).ToList();
        if (stops.Count == 0)
            return null;

        if (t.Contains("забор") || t.Contains("отправит"))
            return stops.FirstOrDefault(s => s.Kind == OrderRouteStopKind.SenderPickup)?.ID_OrderRouteStop
                   ?? stops.First().ID_OrderRouteStop;

        if (t.Contains("доставк") || t.Contains("получат"))
            return stops.FirstOrDefault(s => s.Kind == OrderRouteStopKind.RecipientDelivery)?.ID_OrderRouteStop
                   ?? stops.Last().ID_OrderRouteStop;

        if (t.Contains("склад") || t.Contains("хаб"))
            return stops.FirstOrDefault(s => s.Kind == OrderRouteStopKind.Hub)?.ID_OrderRouteStop;

        return null;
    }

    private static (double? lat, double? lon, string? title) ResolveAssignmentMapPoint(ShiftAssignment a)
    {
        if (a.OrderRouteStop?.Address?.Latitude is { } slat && a.OrderRouteStop.Address.Longitude is { } slon)
            return ((double)slat, (double)slon, a.OrderRouteStop.Title ?? a.Notes);

        var order = a.Order;
        if (order == null)
            return (null, null, a.Notes);

        var notes = a.Notes ?? string.Empty;
        if (notes.Contains("забор", StringComparison.OrdinalIgnoreCase) &&
            order.PickupAddress?.Latitude is { } plat && order.PickupAddress.Longitude is { } plon)
            return ((double)plat, (double)plon, notes);

        if (notes.Contains("доставк", StringComparison.OrdinalIgnoreCase) &&
            order.DeliveryAddress?.Latitude is { } dlat && order.DeliveryAddress.Longitude is { } dlon)
            return ((double)dlat, (double)dlon, notes);

        if (order.DeliveryAddress?.Latitude is { } lat && order.DeliveryAddress.Longitude is { } lon)
            return ((double)lat, (double)lon, notes);

        if (order.PickupAddress?.Latitude is { } plat2 && order.PickupAddress.Longitude is { } plon2)
            return ((double)plat2, (double)plon2, notes);

        return (null, null, notes);
    }

    private static ShiftPlanSummaryDto BuildSyntheticPlanFromOrderedStops(
        CourierProfile courier,
        List<ApplyCourierRouteStopRequest> stops,
        List<Order> orders)
    {
        var courierName = $"{courier.User?.FName} {courier.User?.Name}".Trim();
        var dtoStops = new List<ShiftPlanStopDto>();
        foreach (var stop in stops.OrderBy(s => s.Sequence))
        {
            if (!stop.Latitude.HasValue || !stop.Longitude.HasValue)
                continue;

            var order = stop.OrderId is > 0
                ? orders.FirstOrDefault(o => o.ID_Order == stop.OrderId.Value)
                : null;

            dtoStops.Add(new ShiftPlanStopDto
            {
                AssignmentId = 0,
                Sequence = stop.Sequence,
                OrderId = stop.OrderId ?? 0,
                OrderNumber = order?.Order_Number ?? 0,
                Title = stop.Title,
                Latitude = stop.Latitude,
                Longitude = stop.Longitude,
                AddressLine = stop.Title,
                Status = ShiftAssignmentStatus.Pending,
                Stage = ShiftAssignmentStage.LocalUrban,
                StopKind = OrderRouteStopKind.SenderPickup
            });
        }

        return new ShiftPlanSummaryDto
        {
            ShiftPlanId = 0,
            CompanyId = courier.Company_id,
            CourierId = courier.ID_CourierProfile,
            CourierName = string.IsNullOrWhiteSpace(courierName) ? $"Курьер #{courier.ID_CourierProfile}" : courierName,
            Status = ShiftPlanStatus.Active,
            CreatedAt = DateTime.UtcNow,
            Stops = dtoStops,
            BuiltFromAssignedOrders = true,
            RequiresActiveShift = true
        };
    }

    private async Task<ShiftPlanSummaryDto?> EnsureCourierPlanFromAssignedOrdersAsync(
        int courierProfileId,
        CancellationToken cancellationToken)
    {
        var courier = await _context.CourierProfiles
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId, cancellationToken);
        if (courier == null)
            return null;

        var orders = await _context.Orders
            .Include(o => o.RouteStops).ThenInclude(s => s.Address)
            .Include(o => o.RouteStops).ThenInclude(s => s.LogisticsHub)
            .Include(o => o.PickupAddress)
            .Include(o => o.DeliveryAddress)
            .Where(o => o.Courier_id == courierProfileId && o.Delivered_at == null)
            .OrderByDescending(o => o.Priority)
            .ThenBy(o => o.Created_at)
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
            return null;

        var shift = await _context.CourierShifts
            .Where(s => s.Courier_id == courierProfileId && s.TimeEnd == null)
            .OrderByDescending(s => s.TimeStart)
            .FirstOrDefaultAsync(cancellationToken);

        if (shift == null)
            return BuildSyntheticPlanFromOrders(courier, orders);

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var stalePlans = await _context.ShiftPlans
            .Where(p => p.Shift_id == shift.ID_Shift && (p.Status == ShiftPlanStatus.Active || p.Status == ShiftPlanStatus.Draft))
            .ToListAsync(cancellationToken);
        foreach (var stale in stalePlans)
            stale.Status = ShiftPlanStatus.Replanned;

        var vehicle = await _context.Vehicles
            .Where(v => v.Company_id == courier.Company_id && v.CurrentCourier_id == courierProfileId)
            .OrderByDescending(v => v.ID_Vehicle)
            .FirstOrDefaultAsync(cancellationToken);

        var nextVersion = (await _context.ShiftPlans
            .Where(p => p.Shift_id == shift.ID_Shift)
            .Select(p => (int?)p.Version)
            .MaxAsync(cancellationToken) ?? 0) + 1;

        var plan = new ShiftPlan
        {
            Company_id = courier.Company_id,
            Shift_id = shift.ID_Shift,
            Courier_id = courierProfileId,
            Vehicle_id = vehicle?.ID_Vehicle,
            Status = ShiftPlanStatus.Active,
            Created_at = now,
            Activated_at = now,
            Planned_start_utc = now,
            Version = nextVersion,
            Last_recompute_reason = "courier.assigned_orders"
        };
        _context.ShiftPlans.Add(plan);
        await _context.SaveChangesAsync(cancellationToken);

        var sequence = 1;
        foreach (var order in orders)
        {
            var routeStops = order.RouteStops
                .Where(s => s.Status != OrderRouteStopStatus.Completed)
                .OrderBy(s => s.SortOrder)
                .ToList();

            if (routeStops.Count == 0)
            {
                if (order.PickupAddress != null)
                {
                    _context.ShiftAssignments.Add(CreateAssignment(
                        plan, shift, order, sequence++, ShiftAssignmentStage.LocalUrban,
                        null, "Забор у отправителя"));
                }

                if (order.DeliveryAddress != null)
                {
                    _context.ShiftAssignments.Add(CreateAssignment(
                        plan, shift, order, sequence++, ShiftAssignmentStage.LocalUrban,
                        null, "Доставка получателю"));
                }

                continue;
            }

            foreach (var stop in routeStops)
            {
                var stage = ResolveStageForStop(order, stop);
                _context.ShiftAssignments.Add(CreateAssignment(
                    plan, shift, order, sequence++, stage, stop.ID_OrderRouteStop, stop.Title));
            }

            order.Plan_locked_shiftPlan_id = plan.ID_ShiftPlan;
            order.Plan_locked_at = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var loaded = await _context.ShiftPlans
            .Include(p => p.CourierProfile).ThenInclude(c => c.User)
            .Include(p => p.Vehicle)
            .Include(p => p.Assignments).ThenInclude(a => a.Order)
            .Include(p => p.Assignments).ThenInclude(a => a.OrderRouteStop)!.ThenInclude(s => s!.Address)
            .FirstAsync(p => p.ID_ShiftPlan == plan.ID_ShiftPlan, cancellationToken);

        var dto = MapPlan(loaded);
        dto.BuiltFromAssignedOrders = true;
        return dto;
    }

    private static ShiftAssignment CreateAssignment(
        ShiftPlan plan,
        CourierShift shift,
        Order order,
        int sequence,
        ShiftAssignmentStage stage,
        int? routeStopId,
        string? notes)
    {
        return new ShiftAssignment
        {
            Company_id = plan.Company_id,
            Shift_id = shift.ID_Shift,
            ShiftPlan_id = plan.ID_ShiftPlan,
            Order_id = order.ID_Order,
            Assignment_sequence = sequence,
            OrderRouteStop_id = routeStopId,
            Stage = stage,
            Status = ShiftAssignmentStatus.Pending,
            Planned_start_utc = DateTime.UtcNow,
            Notes = notes
        };
    }

    private static ShiftAssignmentStage ResolveStageForStop(Order order, OrderRouteStop stop)
    {
        if (order.DeliveryRouteKind != DeliveryRouteKind.ViaHub)
            return ShiftAssignmentStage.LocalUrban;

        return stop.Kind switch
        {
            OrderRouteStopKind.SenderPickup => ShiftAssignmentStage.PickupToHub,
            OrderRouteStopKind.Hub when order.HandoffStage is OrderHandoffStage.AtHub or OrderHandoffStage.LastMileInProgress
                => ShiftAssignmentStage.HubToRecipient,
            OrderRouteStopKind.Hub => ShiftAssignmentStage.PickupToHub,
            OrderRouteStopKind.RecipientDelivery => ShiftAssignmentStage.HubToRecipient,
            _ => ShiftAssignmentStage.LocalUrban
        };
    }

    private static ShiftPlanSummaryDto BuildSyntheticPlanFromOrders(CourierProfile courier, List<Order> orders)
    {
        var courierName = $"{courier.User?.FName} {courier.User?.Name}".Trim();
        var stops = new List<ShiftPlanStopDto>();
        var sequence = 1;

        foreach (var order in orders)
        {
            var routeStops = order.RouteStops.OrderBy(s => s.SortOrder).ToList();
            if (routeStops.Count == 0)
            {
                if (order.PickupAddress != null)
                {
                    stops.Add(BuildSyntheticStop(sequence++, order, OrderRouteStopKind.SenderPickup,
                        "Забор у отправителя", order.PickupAddress, ShiftAssignmentStage.LocalUrban));
                }

                if (order.DeliveryAddress != null)
                {
                    stops.Add(BuildSyntheticStop(sequence++, order, OrderRouteStopKind.RecipientDelivery,
                        "Доставка получателю", order.DeliveryAddress, ShiftAssignmentStage.LocalUrban));
                }

                continue;
            }

            foreach (var stop in routeStops)
            {
                var stage = ResolveStageForStop(order, stop);
                stops.Add(new ShiftPlanStopDto
                {
                    AssignmentId = 0,
                    Sequence = sequence++,
                    OrderId = order.ID_Order,
                    OrderNumber = order.Order_Number,
                    OrderRouteStopId = stop.ID_OrderRouteStop,
                    StopKind = stop.Kind,
                    Stage = stage,
                    Status = ShiftAssignmentStatus.Pending,
                    Title = string.IsNullOrWhiteSpace(stop.Title) ? stop.Kind.ToString() : stop.Title,
                    Latitude = stop.Address?.Latitude is { } lat ? (double?)lat : null,
                    Longitude = stop.Address?.Longitude is { } lon ? (double?)lon : null,
                    AddressLine = BuildAddressLine(stop.Address),
                    Priority = order.Priority,
                    OrderSlaDueAtUtc = order.Sla_due_at
                });
            }
        }

        return new ShiftPlanSummaryDto
        {
            ShiftPlanId = 0,
            CompanyId = courier.Company_id,
            CourierId = courier.ID_CourierProfile,
            CourierName = string.IsNullOrWhiteSpace(courierName) ? $"Курьер #{courier.ID_CourierProfile}" : courierName,
            Status = ShiftPlanStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            Stops = stops,
            BuiltFromAssignedOrders = true,
            RequiresActiveShift = true
        };
    }

    private static ShiftPlanStopDto BuildSyntheticStop(
        int sequence,
        Order order,
        OrderRouteStopKind kind,
        string title,
        Address address,
        ShiftAssignmentStage stage)
        => new()
        {
            AssignmentId = 0,
            Sequence = sequence,
            OrderId = order.ID_Order,
            OrderNumber = order.Order_Number,
            StopKind = kind,
            Stage = stage,
            Status = ShiftAssignmentStatus.Pending,
            Title = title,
            Latitude = address.Latitude is { } lat ? (double?)lat : null,
            Longitude = address.Longitude is { } lon ? (double?)lon : null,
            AddressLine = BuildAddressLine(address),
            Priority = order.Priority,
            OrderSlaDueAtUtc = order.Sla_due_at
        };

    public async Task<bool> IsCourierOwnedByUserAsync(int courierProfileId, int userId, CancellationToken cancellationToken = default)
    {
        return await _context.CourierProfiles.AsNoTracking()
            .AnyAsync(c => c.ID_CourierProfile == courierProfileId && c.User_id == userId, cancellationToken);
    }

    private static bool TryPlanOrder(Order order, List<CourierRouteState> states, DateTime nowUtc)
    {
        if (order.DeliveryRouteKind == DeliveryRouteKind.ViaHub)
            return TryPlanViaHub(order, states, nowUtc);
        return TryPlanSingleCourier(order, states, nowUtc);
    }

    private static bool TryPlanSingleCourier(Order order, List<CourierRouteState> states, DateTime nowUtc)
    {
        var reqWeight = Math.Max(order.Weight, ComputeVolumetricWeight(order.Length, order.Width, order.Height));
        var reqVolume = ComputeVolumeM3(order.Length, order.Width, order.Height);
        var isIntercity = !string.Equals(order.PickupAddress?.City?.Trim(), order.DeliveryAddress?.City?.Trim(), StringComparison.OrdinalIgnoreCase);
        var requiresVehicle = isIntercity || reqWeight >= 80m || reqVolume >= 1.5m;
        return TryAddLegToBestCourier(states, new PlannedLeg
        {
            Order = order,
            Stage = ShiftAssignmentStage.LocalUrban,
            StartAddress = order.PickupAddress,
            EndAddress = order.DeliveryAddress,
            LoadWeightKg = reqWeight,
            LoadVolumeM3 = reqVolume,
            RequiresVehicle = requiresVehicle
        }, nowUtc);
    }

    private static bool TryPlanViaHub(Order order, List<CourierRouteState> states, DateTime nowUtc)
    {
        var reqWeight = Math.Max(order.Weight, ComputeVolumetricWeight(order.Length, order.Width, order.Height));
        var reqVolume = ComputeVolumeM3(order.Length, order.Width, order.Height);
        var originAddr = order.OriginHub?.Address;
        var destAddr = order.DestinationHub?.Address;
        if (originAddr == null || destAddr == null)
            return false;

        if (order.HandoffStage is OrderHandoffStage.None or OrderHandoffStage.AwaitingHubDropOff)
        {
            return TryAddLegToBestCourier(states, new PlannedLeg
            {
                Order = order,
                Stage = ShiftAssignmentStage.PickupToHub,
                StartAddress = order.PickupAddress,
                EndAddress = originAddr,
                LoadWeightKg = reqWeight,
                LoadVolumeM3 = reqVolume,
                RequiresVehicle = reqWeight >= 80m || reqVolume >= 1.5m,
                Notes = "Довоз до хаба отправления"
            }, nowUtc);
        }

        if (order.HandoffStage == OrderHandoffStage.AtHub)
        {
            var needHubToHub = order.OriginHub_id.HasValue && order.DestinationHub_id.HasValue &&
                               order.OriginHub_id.Value != order.DestinationHub_id.Value;
            if (needHubToHub)
            {
                return TryAddLegToBestCourier(states, new PlannedLeg
                {
                    Order = order,
                    Stage = ShiftAssignmentStage.HubToHub,
                    StartAddress = originAddr,
                    EndAddress = destAddr,
                    LoadWeightKg = reqWeight,
                    LoadVolumeM3 = reqVolume,
                    RequiresVehicle = true,
                    Notes = "Межхабовая перевозка"
                }, nowUtc);
            }

            return TryAddLegToBestCourier(states, new PlannedLeg
            {
                Order = order,
                Stage = ShiftAssignmentStage.HubToRecipient,
                StartAddress = destAddr,
                EndAddress = order.DeliveryAddress,
                LoadWeightKg = reqWeight,
                LoadVolumeM3 = reqVolume,
                RequiresVehicle = reqWeight >= 80m || reqVolume >= 1.5m,
                Notes = "Последняя миля от хаба"
            }, nowUtc);
        }

        if (order.HandoffStage == OrderHandoffStage.LastMileInProgress)
        {
            return TryAddLegToBestCourier(states, new PlannedLeg
            {
                Order = order,
                Stage = ShiftAssignmentStage.HubToRecipient,
                StartAddress = destAddr,
                EndAddress = order.DeliveryAddress,
                LoadWeightKg = reqWeight,
                LoadVolumeM3 = reqVolume,
                RequiresVehicle = reqWeight >= 80m || reqVolume >= 1.5m,
                Notes = "Последняя миля от хаба"
            }, nowUtc);
        }

        return false;
    }

    private static bool TryAddLegToBestCourier(List<CourierRouteState> states, PlannedLeg leg, DateTime nowUtc)
    {
        CourierRouteState? winner = null;
        CourierRouteState? winnerProjection = null;
        decimal best = decimal.MaxValue;
        foreach (var state in states)
        {
            if (!state.IsCourierOnline)
                continue;
            if (leg.Order.Courier_id.HasValue && leg.Order.Courier_id.Value != state.Shift.Courier_id)
                continue;
            if (leg.RequiresVehicle && !state.HasOperationalVehicle)
                continue;

            var projection = state.Clone(nowUtc);
            if (!projection.TryAddLeg(leg, nowUtc))
                continue;

            var projectedDistanceDelta = Math.Max(0m, projection.TotalDistanceKm - state.TotalDistanceKm);
            var loadWeightRatio = projection.MaxWeightKg > 0 ? projection.PeakWeightKg / projection.MaxWeightKg : 1m;
            var loadVolumeRatio = projection.MaxVolumeM3 > 0 ? projection.PeakVolumeM3 / projection.MaxVolumeM3 : 1m;
            var loadPenalty = Math.Max(loadWeightRatio, loadVolumeRatio) * CriticalLoadPenaltyWeight;
            var slaPenalty = leg.Order.Sla_due_at.HasValue && projection.CursorAtUtc > leg.Order.Sla_due_at.Value
                ? (decimal)(projection.CursorAtUtc - leg.Order.Sla_due_at.Value).TotalMinutes * SlaLateMinutePenalty
                : 0m;
            var noVehiclePenalty = projection.HasOperationalVehicle ? 0m : NoVehiclePenalty;
            var criticalUrbanCourierBias = IsCriticalUrbanFastPath(leg.Order) ? -5.5m : 0m;
            var score = projectedDistanceDelta + loadPenalty + slaPenalty + projection.VehicleHealthPenalty + noVehiclePenalty + projection.Legs.Count * 0.35m + criticalUrbanCourierBias;
            if (score < best)
            {
                best = score;
                winner = state;
                winnerProjection = projection;
            }
        }
        if (winner == null || winnerProjection == null)
            return false;
        winner.ApplyFrom(winnerProjection);
        return true;
    }

    private static UnplannedOrderDto ToUnplanned(Order order, string reason)
        => new()
        {
            OrderId = order.ID_Order,
            OrderNumber = order.Order_Number,
            Priority = order.Priority,
            RouteKind = order.DeliveryRouteKind.ToString(),
            Reason = reason
        };

    private static ShiftPlanSummaryDto MapPlan(ShiftPlan plan)
    {
        var vehicleLabel = plan.Vehicle == null
            ? null
            : $"{plan.Vehicle.Brand_name} {plan.Vehicle.Model_name} ({plan.Vehicle.License_plate})".Trim();
        var courierName = $"{plan.CourierProfile?.User?.FName} {plan.CourierProfile?.User?.Name}".Trim();
        var capWeight = plan.Vehicle?.Max_cargo_weight > 0 ? plan.Vehicle.Max_cargo_weight : DefaultWeightKg;
        var capVolume = plan.Vehicle?.Cargo_volume > 0 ? plan.Vehicle.Cargo_volume : DefaultVolumeM3;
        var loadW = capWeight <= 0 ? 0 : Math.Round(plan.Peak_weight_kg / capWeight, 3);
        var loadV = capVolume <= 0 ? 0 : Math.Round(plan.Peak_volume_m3 / capVolume, 3);

        return new ShiftPlanSummaryDto
        {
            ShiftPlanId = plan.ID_ShiftPlan,
            CompanyId = plan.Company_id,
            ShiftId = plan.Shift_id,
            CourierId = plan.Courier_id,
            CourierName = string.IsNullOrWhiteSpace(courierName) ? $"Курьер #{plan.Courier_id}" : courierName,
            VehicleId = plan.Vehicle_id,
            VehicleLabel = vehicleLabel,
            Status = plan.Status,
            Version = plan.Version,
            LastRecomputeReason = plan.Last_recompute_reason,
            CreatedAt = plan.Created_at,
            PlannedStartUtc = plan.Planned_start_utc,
            PlannedEndUtc = plan.Planned_end_utc,
            TotalDistanceKm = plan.Total_distance_km,
            EstimatedDurationMinutes = plan.Estimated_duration_minutes,
            PeakWeightKg = plan.Peak_weight_kg,
            PeakVolumeM3 = plan.Peak_volume_m3,
            CapacityWeightKg = capWeight,
            CapacityVolumeM3 = capVolume,
            LoadFactorWeight = loadW,
            LoadFactorVolume = loadV,
            Stops = plan.Assignments
                .OrderBy(a => a.Assignment_sequence)
                .Select(a => new ShiftPlanStopDto
                {
                    AssignmentId = a.ID_ShiftAssignment,
                    Sequence = a.Assignment_sequence,
                    OrderId = a.Order_id,
                    OrderNumber = a.Order?.Order_Number ?? 0,
                    OrderRouteStopId = a.OrderRouteStop_id,
                    StopKind = a.OrderRouteStop?.Kind ?? OrderRouteStopKind.SenderPickup,
                    Stage = a.Stage,
                    Status = a.Status,
                    Title = a.OrderRouteStop?.Title,
                    PlannedStartUtc = a.Planned_start_utc,
                    PlannedEndUtc = a.Planned_end_utc,
                    SegmentDistanceKm = a.Planned_distance_km,
                    Latitude = a.OrderRouteStop?.Address?.Latitude is { } lat ? (double?)lat : null,
                    Longitude = a.OrderRouteStop?.Address?.Longitude is { } lon ? (double?)lon : null,
                    AddressLine = BuildAddressLine(a.OrderRouteStop?.Address),
                    HubId = a.OrderRouteStop?.LogisticsHub_id,
                    HubName = a.OrderRouteStop?.LogisticsHub?.Name,
                    Priority = a.Order?.Priority ?? 0,
                    OrderSlaDueAtUtc = a.Order?.Sla_due_at,
                    Notes = a.Notes
                })
                .ToList()
        };
    }

    private static string? BuildAddressLine(Address? a)
    {
        if (a == null) return null;
        var city = string.IsNullOrWhiteSpace(a.City) ? string.Empty : $"{a.City}, ";
        return $"{city}{a.Street} {a.House}".Trim().Trim(',');
    }

    private static decimal ComputeVolumetricWeight(decimal lengthCm, decimal widthCm, decimal heightCm)
        => Math.Max(0m, (lengthCm * widthCm * heightCm) / 5000m);

    private static decimal ComputeVolumeM3(decimal lengthCm, decimal widthCm, decimal heightCm)
    {
        var l = Math.Max(0m, lengthCm) / 100m;
        var w = Math.Max(0m, widthCm) / 100m;
        var h = Math.Max(0m, heightCm) / 100m;
        return l * w * h;
    }

    private static (double lat, double lon)? GetPoint(Address? address)
    {
        if (address?.Latitude is not { } lat || address.Longitude is not { } lon)
            return null;
        return ((double)lat, (double)lon);
    }

    /// <summary>
    /// Критически срочный заказ в «локальной» связке (один населённый пункт по справочнику или короткое плечо А→Б):
    /// для таких маршрутизатор стремится завершить заказ как можно раньше. Иначе порядок точек оптимизируется по расстоянию (топливо).
    /// </summary>
    private static bool IsCriticalUrbanFastPath(Order order)
    {
        if (order.Priority != OrderPriorityCriticallyUrgent)
            return false;
        var pickup = order.PickupAddress;
        var delivery = order.DeliveryAddress;
        if (pickup == null || delivery == null)
            return false;

        if (!string.IsNullOrWhiteSpace(pickup.City) && !string.IsNullOrWhiteSpace(delivery.City) &&
            string.Equals(pickup.City.Trim(), delivery.City.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;

        var pk = GetPoint(pickup);
        var dk = GetPoint(delivery);
        if (pk == null || dk == null)
            return false;

        return DistKm(pk.Value, dk.Value) <= CriticalUrbanClusterMaxDirectKm;
    }

    /// <summary>
    /// Близкие точки забора: усиливаем заборы в кластере (снижаем score только у кандидатов-заборов), доставки не штрафуем.
    /// </summary>
    private static decimal PickupClusterPickupAffinityBiasKm(
        RouteNode candidate,
        HashSet<int> pickedLegIndices,
        IReadOnlyList<PlannedLeg> legs)
    {
        if (!candidate.IsPickup)
            return 0m;
        var i = candidate.LegIndex;
        var pickI = GetPoint(legs[i].StartAddress);
        if (pickI == null)
            return 0m;

        var neighbors = 0;
        for (var j = 0; j < legs.Count; j++)
        {
            if (j == i || pickedLegIndices.Contains(j))
                continue;
            var pickJ = GetPoint(legs[j].StartAddress);
            if (pickJ == null)
                continue;
            if (DistKm(pickI.Value, pickJ.Value) <= PickupClusterMaxKm)
                neighbors++;
        }

        return neighbors == 0 ? 0m : -(neighbors * PickupClusterPickupAffinityKmPerNeighbor);
    }

    private static decimal DistKm((double lat, double lon) a, (double lat, double lon) b)
    {
        const double r = 6371.0;
        var dLat = ToRad(b.lat - a.lat);
        var dLon = ToRad(b.lon - a.lon);
        var x = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(a.lat)) * Math.Cos(ToRad(b.lat)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(x), Math.Sqrt(1 - x));
        return (decimal)(r * c);
    }

    private static double ToRad(double v) => v * (Math.PI / 180.0);

    private static bool IsVehicleOperational(Vehicle? v)
    {
        if (v == null || !v.Is_available)
            return false;

        var now = DateTime.UtcNow;
        if (v.Maintenance_due_at.HasValue && v.Maintenance_due_at.Value <= now)
            return false;
        if (v.Insurance_expires_at.HasValue && v.Insurance_expires_at.Value <= now)
            return false;
        if (v.Registration_expires_at.HasValue && v.Registration_expires_at.Value <= now)
            return false;
        return true;
    }

    private static decimal ComputeVehicleHealthPenalty(Vehicle? v)
    {
        if (v == null)
            return 1.8m;

        var now = DateTime.UtcNow;
        decimal penalty = v.Is_available ? 0m : 4m;

        if (v.Maintenance_due_at.HasValue)
        {
            penalty += v.Maintenance_due_at.Value <= now
                ? 5m
                : v.Maintenance_due_at.Value <= now.AddDays(7) ? 0.8m : 0m;
        }
        if (v.Insurance_expires_at.HasValue)
        {
            penalty += v.Insurance_expires_at.Value <= now
                ? 5m
                : v.Insurance_expires_at.Value <= now.AddDays(7) ? 0.8m : 0m;
        }
        if (v.Registration_expires_at.HasValue)
        {
            penalty += v.Registration_expires_at.Value <= now
                ? 5m
                : v.Registration_expires_at.Value <= now.AddDays(7) ? 0.8m : 0m;
        }

        return penalty;
    }

    private sealed class CourierRouteState
    {
        public required CourierShift Shift { get; init; }
        public Vehicle? Vehicle { get; init; }
        public required decimal MaxWeightKg { get; init; }
        public required decimal MaxVolumeM3 { get; init; }
        public required bool IsCourierOnline { get; init; }
        public required bool HasOperationalVehicle { get; init; }
        public required decimal VehicleHealthPenalty { get; init; }
        public decimal CurrentWeightKg { get; private set; }
        public decimal CurrentVolumeM3 { get; private set; }
        public decimal PeakWeightKg { get; private set; }
        public decimal PeakVolumeM3 { get; private set; }
        public double? CurrentLat { get; init; }
        public double? CurrentLon { get; init; }
        public required DateTime CursorAtUtc { get; set; }
        public List<PlannedStop> Stops { get; } = new();
        public List<PlannedLeg> Legs { get; } = new();
        public decimal TotalDistanceKm => Stops.Sum(s => s.DistanceKm);

        public (double lat, double lon)? ResolveAnchor()
        {
            if (Stops.Count > 0)
                return Stops.Last().Point;
            if (CurrentLat.HasValue && CurrentLon.HasValue)
                return (CurrentLat.Value, CurrentLon.Value);
            return null;
        }

        public CourierRouteState Clone(DateTime nowUtc)
        {
            var clone = new CourierRouteState
            {
                Shift = Shift,
                Vehicle = Vehicle,
                MaxWeightKg = MaxWeightKg,
                MaxVolumeM3 = MaxVolumeM3,
                CurrentLat = CurrentLat,
                CurrentLon = CurrentLon,
                IsCourierOnline = IsCourierOnline,
                HasOperationalVehicle = HasOperationalVehicle,
                VehicleHealthPenalty = VehicleHealthPenalty,
                CursorAtUtc = nowUtc
            };
            clone.Legs.AddRange(Legs);
            _ = clone.RebuildStops(nowUtc);
            return clone;
        }

        public void ApplyFrom(CourierRouteState other)
        {
            Legs.Clear();
            Legs.AddRange(other.Legs);
            Stops.Clear();
            Stops.AddRange(other.Stops);
            CurrentWeightKg = other.CurrentWeightKg;
            CurrentVolumeM3 = other.CurrentVolumeM3;
            PeakWeightKg = other.PeakWeightKg;
            PeakVolumeM3 = other.PeakVolumeM3;
            CursorAtUtc = other.CursorAtUtc;
        }

        public bool TryAddLeg(PlannedLeg leg, DateTime nowUtc)
        {
            if (GetPoint(leg.StartAddress) == null || GetPoint(leg.EndAddress) == null)
                return false;
            Legs.Add(leg);
            return RebuildStops(nowUtc);
        }

        private bool RebuildStops(DateTime nowUtc)
        {
            Stops.Clear();
            CurrentWeightKg = 0;
            CurrentVolumeM3 = 0;
            PeakWeightKg = 0;
            PeakVolumeM3 = 0;
            CursorAtUtc = nowUtc;

            var nodes = new List<RouteNode>();
            for (var i = 0; i < Legs.Count; i++)
            {
                nodes.Add(RouteNode.CreatePickup(i, Legs[i]));
                nodes.Add(RouteNode.CreateDrop(i, Legs[i]));
            }

            var done = new HashSet<int>();
            var picked = new HashSet<int>();
            var currentPoint = ResolveAnchor();
            if (currentPoint == null)
                return false;

            while (done.Count < nodes.Count)
            {
                var available = nodes
                    .Where(n => !done.Contains(n.NodeId))
                    .Where(n => n.IsPickup || picked.Contains(n.LegIndex))
                    .Where(n => !n.IsPickup || (CurrentWeightKg + n.Leg.LoadWeightKg <= MaxWeightKg && CurrentVolumeM3 + n.Leg.LoadVolumeM3 <= MaxVolumeM3))
                    .ToList();
                if (available.Count == 0)
                    return false;

                var next = available
                    .Select(n =>
                    {
                        var p = GetPoint(n.Address)!.Value;
                        var distanceScore = DistKm(currentPoint.Value, p);
                        var slaPenalty = n.Leg.Order.Sla_due_at.HasValue
                            ? Math.Max(0m, (decimal)(DateTime.UtcNow - n.Leg.Order.Sla_due_at.Value).TotalMinutes) * 0.02m
                            : 0m;
                        // Критически срочный + городская связка: сдвигаем забор и вручение этого заказа раньше по маршруту.
                        // Для длинных межгородских плечей — только расстояние (экономия топлива), порядок остальных точек гибкий.
                        var rushBiasKm = 0m;
                        if (IsCriticalUrbanFastPath(n.Leg.Order))
                        {
                            if (n.IsPickup)
                                rushBiasKm = -CriticalUrbanPickupScoreBiasKm;
                            else if (picked.Contains(n.LegIndex))
                                rushBiasKm = -CriticalUrbanDropScoreBiasKm;
                        }

                        var pickupClusterBiasKm = PickupClusterPickupAffinityBiasKm(n, picked, Legs);

                        return new { Node = n, Score = distanceScore + slaPenalty + rushBiasKm + pickupClusterBiasKm };
                    })
                    .OrderBy(x => x.Score)
                    .First()
                    .Node;

                var nextPoint = GetPoint(next.Address)!.Value;
                var legDist = DistKm(currentPoint.Value, nextPoint);
                var durMinutes = legDist / Math.Max(10m, AvgSpeedKmH) * 60m + StopServiceMinutes;
                var startAt = Stops.Count == 0 ? nowUtc : Stops.Last().PlannedEndUtc;
                var endAt = startAt.AddMinutes((double)durMinutes);

                if (next.IsPickup)
                {
                    CurrentWeightKg += next.Leg.LoadWeightKg;
                    CurrentVolumeM3 += next.Leg.LoadVolumeM3;
                    picked.Add(next.LegIndex);
                }
                else
                {
                    CurrentWeightKg = Math.Max(0m, CurrentWeightKg - next.Leg.LoadWeightKg);
                    CurrentVolumeM3 = Math.Max(0m, CurrentVolumeM3 - next.Leg.LoadVolumeM3);
                }

                PeakWeightKg = Math.Max(PeakWeightKg, CurrentWeightKg);
                PeakVolumeM3 = Math.Max(PeakVolumeM3, CurrentVolumeM3);

                Stops.Add(new PlannedStop
                {
                    Sequence = Stops.Count + 1,
                    Order = next.Leg.Order,
                    Stage = next.Leg.Stage,
                    Point = nextPoint,
                    PlannedStartUtc = startAt,
                    PlannedEndUtc = endAt,
                    DistanceKm = legDist,
                    DurationMinutes = durMinutes,
                    OrderRouteStopId = ResolveRouteStopId(next.Leg, next.IsPickup),
                    Notes = next.Leg.Notes
                });

                done.Add(next.NodeId);
                currentPoint = nextPoint;
                CursorAtUtc = endAt;
            }

            return true;
        }

        private static int? ResolveRouteStopId(PlannedLeg leg, bool isPickup)
        {
            var routeStops = leg.Order.RouteStops.OrderBy(s => s.SortOrder).ToList();
            if (routeStops.Count == 0)
                return null;

            return leg.Stage switch
            {
                ShiftAssignmentStage.LocalUrban => isPickup
                    ? routeStops.FirstOrDefault(s => s.Kind == OrderRouteStopKind.SenderPickup)?.ID_OrderRouteStop
                    : routeStops.FirstOrDefault(s => s.Kind == OrderRouteStopKind.RecipientDelivery)?.ID_OrderRouteStop,
                ShiftAssignmentStage.PickupToHub => isPickup
                    ? routeStops.FirstOrDefault(s => s.Kind == OrderRouteStopKind.SenderPickup)?.ID_OrderRouteStop
                    : routeStops.FirstOrDefault(s => s.Kind == OrderRouteStopKind.Hub)?.ID_OrderRouteStop,
                ShiftAssignmentStage.HubToHub => isPickup
                    ? routeStops.FirstOrDefault(s => s.Kind == OrderRouteStopKind.Hub)?.ID_OrderRouteStop
                    : routeStops.LastOrDefault(s => s.Kind == OrderRouteStopKind.Hub)?.ID_OrderRouteStop,
                ShiftAssignmentStage.HubToRecipient => isPickup
                    ? routeStops.LastOrDefault(s => s.Kind == OrderRouteStopKind.Hub)?.ID_OrderRouteStop
                    : routeStops.FirstOrDefault(s => s.Kind == OrderRouteStopKind.RecipientDelivery)?.ID_OrderRouteStop,
                _ => routeStops.FirstOrDefault()?.ID_OrderRouteStop
            };
        }
    }

    private sealed class PlannedLeg
    {
        public required Order Order { get; init; }
        public required ShiftAssignmentStage Stage { get; init; }
        public required Address? StartAddress { get; init; }
        public required Address? EndAddress { get; init; }
        public decimal LoadWeightKg { get; init; }
        public decimal LoadVolumeM3 { get; init; }
        public bool RequiresVehicle { get; init; }
        public string? Notes { get; init; }
    }

    private sealed class RouteNode
    {
        public int NodeId { get; init; }
        public int LegIndex { get; init; }
        public required PlannedLeg Leg { get; init; }
        public required Address? Address { get; init; }
        public bool IsPickup { get; init; }

        public static RouteNode CreatePickup(int legIndex, PlannedLeg leg)
            => new() { NodeId = legIndex * 2, LegIndex = legIndex, Leg = leg, Address = leg.StartAddress, IsPickup = true };

        public static RouteNode CreateDrop(int legIndex, PlannedLeg leg)
            => new() { NodeId = legIndex * 2 + 1, LegIndex = legIndex, Leg = leg, Address = leg.EndAddress, IsPickup = false };
    }

    private sealed class PlannedStop
    {
        public int Sequence { get; init; }
        public required Order Order { get; init; }
        public required ShiftAssignmentStage Stage { get; init; }
        public required (double lat, double lon) Point { get; init; }
        public DateTime PlannedStartUtc { get; init; }
        public DateTime PlannedEndUtc { get; init; }
        public decimal DistanceKm { get; init; }
        public decimal DurationMinutes { get; init; }
        public int? OrderRouteStopId { get; init; }
        public string? Notes { get; init; }
    }

    public async Task<ShiftClosureSummaryDto?> FinalizeShiftAsync(int shiftId, CancellationToken cancellationToken = default)
    {
        var shift = await _context.CourierShifts
            .Include(s => s.CourierProfile).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(s => s.ID_Shift == shiftId, cancellationToken);
        if (shift == null)
            return null;

        var plans = await _context.ShiftPlans
            .Where(p => p.Shift_id == shiftId)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var plan in plans.Where(p => p.Status is ShiftPlanStatus.Active or ShiftPlanStatus.Draft))
        {
            plan.Status = ShiftPlanStatus.Completed;
            plan.Completed_at = now;
        }

        var assignments = await _context.ShiftAssignments
            .Include(a => a.Order).ThenInclude(o => o!.PickupAddress)
            .Include(a => a.Order).ThenInclude(o => o!.DeliveryAddress)
            .Include(a => a.OrderRouteStop)!.ThenInclude(s => s!.Address)
            .Where(a => a.Shift_id == shiftId)
            .ToListAsync(cancellationToken);

        var finalPlan = plans
            .Where(p => p.Status == ShiftPlanStatus.Completed)
            .OrderByDescending(p => p.Version)
            .ThenByDescending(p => p.ID_ShiftPlan)
            .FirstOrDefault();

        if (finalPlan != null)
        {
            var planAssignments = assignments
                .Where(a => a.ShiftPlan_id == finalPlan.ID_ShiftPlan)
                .OrderBy(a => a.Assignment_sequence)
                .ToList();
            if (planAssignments.Count == 0)
                planAssignments.AddRange(assignments.OrderBy(a => a.Assignment_sequence));

            RecalculateAssignmentSegmentDistances(planAssignments);
            finalPlan.Total_distance_km = Math.Round(planAssignments.Sum(a => a.Planned_distance_km), 3);
            finalPlan.Estimated_duration_minutes = Math.Round(
                planAssignments.Count * StopServiceMinutes + finalPlan.Total_distance_km / AvgSpeedKmH * 60m, 1);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await BuildShiftClosureSummaryAsync(shift, finalPlan, assignments, cancellationToken);
    }

    public async Task<ShiftClosureSummaryDto?> GetShiftClosureSummaryAsync(int shiftId, CancellationToken cancellationToken = default)
    {
        var shift = await _context.CourierShifts
            .Include(s => s.CourierProfile).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(s => s.ID_Shift == shiftId, cancellationToken);
        if (shift == null)
            return null;

        var finalPlan = await _context.ShiftPlans
            .Where(p => p.Shift_id == shiftId && p.Status == ShiftPlanStatus.Completed)
            .OrderByDescending(p => p.Version)
            .ThenByDescending(p => p.ID_ShiftPlan)
            .FirstOrDefaultAsync(cancellationToken);

        var assignments = await _context.ShiftAssignments
            .Include(a => a.Order)
            .Where(a => a.Shift_id == shiftId)
            .ToListAsync(cancellationToken);

        return await BuildShiftClosureSummaryAsync(shift, finalPlan, assignments, cancellationToken);
    }

    public async Task RecalculateActivePlanDistanceAsync(int courierProfileId, CancellationToken cancellationToken = default)
    {
        var plan = await _context.ShiftPlans
            .Include(p => p.Assignments).ThenInclude(a => a.Order).ThenInclude(o => o!.PickupAddress)
            .Include(p => p.Assignments).ThenInclude(a => a.Order).ThenInclude(o => o!.DeliveryAddress)
            .Include(p => p.Assignments).ThenInclude(a => a.OrderRouteStop)!.ThenInclude(s => s!.Address)
            .Where(p => p.Courier_id == courierProfileId && (p.Status == ShiftPlanStatus.Active || p.Status == ShiftPlanStatus.Draft))
            .OrderByDescending(p => p.Created_at)
            .FirstOrDefaultAsync(cancellationToken);

        if (plan == null)
            return;

        var ordered = plan.Assignments
            .Where(a => a.Status is ShiftAssignmentStatus.Pending or ShiftAssignmentStatus.InProgress or ShiftAssignmentStatus.Done)
            .OrderBy(a => a.Assignment_sequence)
            .ToList();

        RecalculateAssignmentSegmentDistances(ordered);
        plan.Total_distance_km = Math.Round(ordered.Sum(a => a.Planned_distance_km), 3);
        plan.Estimated_duration_minutes = Math.Round(
            ordered.Count * StopServiceMinutes + plan.Total_distance_km / AvgSpeedKmH * 60m, 1);
        plan.Last_recompute_reason = "route.recalculated";

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CourierRouteMapWaypointDto>> GetShiftRouteWaypointsAsync(
        int shiftId,
        CancellationToken cancellationToken = default)
    {
        var plan = await _context.ShiftPlans
            .Include(p => p.Assignments).ThenInclude(a => a.OrderRouteStop)!.ThenInclude(s => s!.Address)
            .Include(p => p.Assignments).ThenInclude(a => a.Order).ThenInclude(o => o!.PickupAddress)
            .Include(p => p.Assignments).ThenInclude(a => a.Order).ThenInclude(o => o!.DeliveryAddress)
            .Where(p => p.Shift_id == shiftId && p.Status == ShiftPlanStatus.Completed)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (plan == null)
            return Array.Empty<CourierRouteMapWaypointDto>();

        var list = new List<CourierRouteMapWaypointDto>();
        foreach (var a in plan.Assignments.OrderBy(x => x.Assignment_sequence))
        {
            var (lat, lon, title) = ResolveAssignmentMapPoint(a);
            if (!lat.HasValue || !lon.HasValue)
                continue;

            list.Add(new CourierRouteMapWaypointDto
            {
                Sequence = a.Assignment_sequence,
                OrderId = a.Order_id > 0 ? a.Order_id : null,
                AssignmentId = a.ID_ShiftAssignment,
                Title = title ?? a.Notes,
                Lat = lat.Value,
                Lon = lon.Value
            });
        }

        return list;
    }

    private async Task<ShiftClosureSummaryDto> BuildShiftClosureSummaryAsync(
        CourierShift shift,
        ShiftPlan? finalPlan,
        List<ShiftAssignment> assignments,
        CancellationToken cancellationToken)
    {
        var courier = shift.CourierProfile;
        var name = $"{courier?.User?.Name} {courier?.User?.FName}".Trim();
        if (string.IsNullOrWhiteSpace(name))
            name = $"Курьер #{shift.Courier_id}";

        var totalKm = finalPlan?.Total_distance_km ?? Math.Round(assignments.Sum(a => a.Planned_distance_km), 3);
        if (totalKm <= 0)
        {
            var ordered = assignments.OrderBy(a => a.Assignment_sequence).ToList();
            RecalculateAssignmentSegmentDistances(ordered);
            totalKm = Math.Round(ordered.Sum(a => a.Planned_distance_km), 3);
        }

        var (liters, costRub) = await EstimateShiftFuelAsync(shift.Company_id, shift.Courier_id, totalKm, cancellationToken);
        var waypoints = await GetShiftRouteWaypointsAsync(shift.ID_Shift, cancellationToken);

        return new ShiftClosureSummaryDto
        {
            ShiftId = shift.ID_Shift,
            CourierProfileId = shift.Courier_id,
            CourierName = name,
            TimeStart = shift.TimeStart,
            TimeEnd = shift.TimeEnd,
            ShiftPlanId = finalPlan?.ID_ShiftPlan,
            PlanVersion = finalPlan?.Version ?? 0,
            TotalDistanceKm = totalKm,
            EstimatedFuelLiters = liters,
            EstimatedFuelCostRub = costRub,
            OrdersCompletedCount = assignments
                .Where(a => a.Status == ShiftAssignmentStatus.Done)
                .Select(a => a.Order_id)
                .Distinct()
                .Count(),
            RouteStopsCompletedCount = assignments.Count(a => a.Status == ShiftAssignmentStatus.Done),
            Waypoints = waypoints.ToList()
        };
    }

    private async Task<(decimal liters, decimal costRub)> EstimateShiftFuelAsync(
        int companyId,
        int courierProfileId,
        decimal distanceKm,
        CancellationToken cancellationToken)
    {
        var safeDistance = Math.Max(0.1m, distanceKm);
        var consumptionL100 = 10.5m;
        var fuelPrice = await _fuelPriceService.GetPriceRubPerLiterAsync(null, cancellationToken);

        var vehicle = await _context.Vehicles
            .AsNoTracking()
            .Include(v => v.VehicleModel)
            .Include(v => v.FuelType)
            .Where(v => v.Company_id == companyId && v.CurrentCourier_id == courierProfileId)
            .OrderByDescending(v => v.ID_Vehicle)
            .FirstOrDefaultAsync(cancellationToken);

        if (vehicle?.VehicleModel?.AvgFuelCity > 0)
            consumptionL100 = vehicle.VehicleModel.AvgFuelCity;

        if (vehicle?.FuelType?.Name != null)
            fuelPrice = await _fuelPriceService.GetPriceRubPerLiterAsync(vehicle.FuelType.Name, cancellationToken);

        var liters = Math.Round(safeDistance * consumptionL100 / 100m, 2);
        var costRub = Math.Round(liters * fuelPrice, 2);
        return (liters, costRub);
    }

    private static void ApplySegmentDistancesToAssignments(
        List<ShiftAssignment> assignments,
        IReadOnlyList<ApplyCourierRouteStopRequest> orderedStops)
    {
        var points = orderedStops
            .Where(s => s.OrderId is > 0 && s.Latitude.HasValue && s.Longitude.HasValue)
            .Select(s => ((double)s.Latitude!.Value, (double)s.Longitude!.Value))
            .ToList();

        for (var i = 0; i < assignments.Count; i++)
        {
            if (i == 0 || i >= points.Count)
            {
                assignments[i].Planned_distance_km = 0;
                continue;
            }

            var prev = points[i - 1];
            var cur = points[i];
            assignments[i].Planned_distance_km = Math.Round((decimal)HaversineKm(prev.Item1, prev.Item2, cur.Item1, cur.Item2), 3);
        }
    }

    private void RecalculateAssignmentSegmentDistances(List<ShiftAssignment> orderedAssignments)
    {
        (double lat, double lon)? prev = null;
        foreach (var a in orderedAssignments)
        {
            var point = ResolveAssignmentCoordinates(a);
            if (!point.HasValue)
            {
                a.Planned_distance_km = 0;
                continue;
            }

            if (!prev.HasValue)
            {
                a.Planned_distance_km = 0;
                prev = point;
                continue;
            }

            a.Planned_distance_km = Math.Round(
                (decimal)HaversineKm(prev.Value.lat, prev.Value.lon, point.Value.lat, point.Value.lon), 3);
            prev = point;
        }
    }

    private static (double lat, double lon)? ResolveAssignmentCoordinates(ShiftAssignment assignment)
    {
        if (assignment.OrderRouteStop?.Address?.Latitude is { } rLat &&
            assignment.OrderRouteStop.Address.Longitude is { } rLon &&
            (rLat != 0 || rLon != 0))
            return ((double)rLat, (double)rLon);

        var order = assignment.Order;
        if (order?.PickupAddress?.Latitude is { } pLat && order.PickupAddress.Longitude is { } pLon && (pLat != 0 || pLon != 0))
            return ((double)pLat, (double)pLon);

        if (order?.DeliveryAddress?.Latitude is { } dLat && order.DeliveryAddress.Longitude is { } dLon && (dLat != 0 || dLon != 0))
            return ((double)dLat, (double)dLon);

        return null;
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6371.0;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }

    private static double DegreesToRadians(double deg) => deg * (Math.PI / 180.0);
}
