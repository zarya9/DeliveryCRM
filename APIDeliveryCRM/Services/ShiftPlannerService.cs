using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Responses;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services;

public class ShiftPlannerService : IShiftPlannerService
{
    private const decimal DefaultVolumeM3 = 2.5m;
    private const decimal DefaultWeightKg = 450m;
    private const decimal AvgSpeedKmH = 28m;
    private const decimal StopServiceMinutes = 8m;

    private readonly ContextDB _context;

    public ShiftPlannerService(ContextDB context)
    {
        _context = context;
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
                unplanned.Add(ToUnplanned(order, "Не хватает вместимости или координат для маршрута"));
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
        return TryAddLegToBestCourier(states, new PlannedLeg
        {
            Order = order,
            Stage = ShiftAssignmentStage.LocalUrban,
            StartAddress = order.PickupAddress,
            EndAddress = order.DeliveryAddress,
            LoadWeightKg = reqWeight,
            LoadVolumeM3 = reqVolume
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
            var projection = state.Clone(nowUtc);
            if (!projection.TryAddLeg(leg, nowUtc))
                continue;

            var d = projection.TotalDistanceKm;
            if (d < best)
            {
                best = d;
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

    private sealed class CourierRouteState
    {
        public required CourierShift Shift { get; init; }
        public Vehicle? Vehicle { get; init; }
        public required decimal MaxWeightKg { get; init; }
        public required decimal MaxVolumeM3 { get; init; }
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
                        return new { Node = n, Score = distanceScore + slaPenalty };
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
}
