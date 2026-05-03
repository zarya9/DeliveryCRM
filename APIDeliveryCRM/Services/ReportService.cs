using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services;

public class ReportService : IReportService
{
    private readonly ContextDB _context;
    private const double FuelLitersPer100Km = 10.0;
    private const double OptimizationSavingsFactor = 0.12;

    public ReportService(ContextDB context)
    {
        _context = context;
    }

    public async Task<IActionResult> GetFinanceDashboardAsync(int companyId, DateTime? fromUtc, DateTime? toUtc)
    {
        var built = await BuildFinanceDashboardAsync(companyId, fromUtc, toUtc);
        return built.Error ?? new OkObjectResult(built.Dto);
    }

    public async Task<IActionResult> ExportFinanceExcelAsync(int companyId, DateTime? fromUtc, DateTime? toUtc)
    {
        var built = await BuildFinanceDashboardAsync(companyId, fromUtc, toUtc);
        if (built.Error != null)
            return built.Error;

        var bytes = FinanceReportExcelBuilder.Build(built.Dto!);
        var fileName = $"finance-report-{built.Dto!.PeriodFromUtc:yyyy-MM-dd}-{built.Dto.PeriodToUtc:yyyy-MM-dd}.xlsx";
        return new FileContentResult(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            FileDownloadName = fileName
        };
    }

    public async Task<IActionResult> ExportFinancePdfAsync(int companyId, DateTime? fromUtc, DateTime? toUtc)
    {
        var built = await BuildFinanceDashboardAsync(companyId, fromUtc, toUtc);
        if (built.Error != null)
            return built.Error;

        var bytes = FinanceReportPdfBuilder.Build(built.Dto!);
        var fileName = $"finance-report-{built.Dto!.PeriodFromUtc:yyyy-MM-dd}-{built.Dto.PeriodToUtc:yyyy-MM-dd}.pdf";
        return new FileContentResult(bytes, "application/pdf")
        {
            FileDownloadName = fileName
        };
    }

    private async Task<(FinanceDashboardResponse? Dto, IActionResult? Error)> BuildFinanceDashboardAsync(
        int companyId,
        DateTime? fromUtc,
        DateTime? toUtc)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID_Company == companyId);
        if (company == null)
            return (null, new NotFoundObjectResult(new { message = "Компания не найдена." }));

        var onTimeHours = company.SlaOnTimeHours > 0 ? company.SlaOnTimeHours : 4;
        var lateHours = company.SlaLateHours > 0 ? company.SlaLateHours : 24;
        if (lateHours < onTimeHours)
            lateHours = onTimeHours;

        var (from, to) = NormalizePeriodUtc(fromUtc, toUtc);

        var ordersCreated = await _context.Orders
            .Where(o => o.Company_id == companyId && o.Created_at >= from && o.Created_at <= to)
            .Include(o => o.OrderStatus)
            .ToListAsync();

        var deliveredInPeriod = await _context.Orders
            .Where(o => o.Company_id == companyId
                        && o.Delivered_at != null
                        && o.Delivered_at >= from
                        && o.Delivered_at <= to)
            .Include(o => o.CourierProfile).ThenInclude(c => c!.User)
            .Include(o => o.ClientProfile).ThenInclude(c => c.User)
            .ToListAsync();

        var couriers = await _context.CourierProfiles
            .Where(c => c.Company_id == companyId)
            .Include(c => c.User)
            .ToListAsync();

        var leads = await _context.Leads
            .Where(l => l.Company_id == companyId && l.Created_at >= from && l.Created_at <= to)
            .Include(l => l.Stage)
            .Include(l => l.Manager)
            .ToListAsync();

        var dto = new FinanceDashboardResponse
        {
            SlaOnTimeHours = onTimeHours,
            SlaLateHours = lateHours,
            PeriodFromUtc = from,
            PeriodToUtc = to
        };

        dto.OrdersCreatedInPeriod = ordersCreated.Count;

        dto.RevenueDeliveredInPeriod = deliveredInPeriod.Sum(o => o.Final_cost > 0 ? o.Final_cost : o.Estimated_cost);
        dto.AvgCheckDeliveredInPeriod = deliveredInPeriod.Count > 0
            ? dto.RevenueDeliveredInPeriod / deliveredInPeriod.Count
            : 0;
        dto.PaidDeliveredCount = deliveredInPeriod.Count(o => o.Is_paid);
        dto.PaidSharePercent = deliveredInPeriod.Count > 0
            ? dto.PaidDeliveredCount * 100m / deliveredInPeriod.Count
            : 0;

        var withDuration = deliveredInPeriod
            .Where(o => o.Delivered_at!.Value > o.Created_at)
            .ToList();

        dto.AvgDeliveryHours = withDuration.Count > 0
            ? withDuration.Average(o => (o.Delivered_at!.Value - o.Created_at).TotalHours)
            : 0;

        var slaLimit = TimeSpan.FromHours(onTimeHours);
        var lateLimit = TimeSpan.FromHours(lateHours);

        var onTimeCount = withDuration.Count(o => (o.Delivered_at!.Value - o.Created_at) <= slaLimit);
        dto.OnTimePercent = withDuration.Count > 0
            ? onTimeCount * 100.0 / withDuration.Count
            : 0;

        var lateCount = withDuration.Count(o => (o.Delivered_at!.Value - o.Created_at) > lateLimit);
        dto.LatePercent = withDuration.Count > 0
            ? lateCount * 100.0 / withDuration.Count
            : 0;

        var routedDistanceKm = deliveredInPeriod
            .Select(EstimateOrderDistanceKm)
            .Where(x => x > 0)
            .Sum();
        var baselineDistanceKm = routedDistanceKm * (1.0 + OptimizationSavingsFactor);
        var savedDistanceKm = Math.Max(0, baselineDistanceKm - routedDistanceKm);
        dto.FuelConsumptionLitersPer100Km = FuelLitersPer100Km;
        dto.EstimatedFuelUsedLiters = routedDistanceKm * FuelLitersPer100Km / 100.0;
        dto.EstimatedFuelSavedLiters = savedDistanceKm * FuelLitersPer100Km / 100.0;
        dto.EstimatedFuelSavingsPercent = baselineDistanceKm > 0
            ? savedDistanceKm * 100.0 / baselineDistanceKm
            : 0;

        dto.OrdersByDay = BuildOrdersByDay(ordersCreated, from, to);
        dto.StatusRows = BuildStatusRows(ordersCreated);

        dto.CourierRows = couriers
            .Select(c =>
            {
                var courierOrders = deliveredInPeriod.Where(o => o.Courier_id == c.ID_CourierProfile).ToList();
                var revenue = courierOrders.Sum(o => o.Final_cost > 0 ? o.Final_cost : o.Estimated_cost);
                var fullName = $"{c.User?.FName} {c.User?.Name}".Trim();
                return new CourierRow
                {
                    Name = string.IsNullOrWhiteSpace(fullName) ? $"Курьер #{c.ID_CourierProfile}" : fullName,
                    Delivered = courierOrders.Count,
                    Revenue = revenue,
                    Rating = c.Rating
                };
            })
            .Where(c => c.Delivered > 0 || c.Revenue > 0)
            .OrderByDescending(c => c.Delivered)
            .ThenByDescending(c => c.Revenue)
            .Take(10)
            .ToList();

        dto.ClientRows = deliveredInPeriod
            .GroupBy(o =>
            {
                var fullName = $"{o.ClientProfile?.User?.FName} {o.ClientProfile?.User?.Name}".Trim();
                return string.IsNullOrWhiteSpace(fullName) ? $"Клиент #{o.Client_id}" : fullName;
            })
            .Select(g => new ClientRow
            {
                Name = g.Key,
                Orders = g.Count(),
                Revenue = g.Sum(x => x.Final_cost > 0 ? x.Final_cost : x.Estimated_cost)
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.Orders)
            .Take(12)
            .ToList();

        dto.ManagerRows = leads
            .GroupBy(l =>
            {
                var fullName = $"{l.Manager?.FName} {l.Manager?.Name}".Trim();
                return string.IsNullOrWhiteSpace(fullName) ? "Не назначен" : fullName;
            })
            .Select(g =>
            {
                var conversions = g.Count(x => IsLeadConvertedStage(x.Stage?.Name));
                return new ManagerEfficiencyRow
                {
                    Manager = g.Key,
                    Leads = g.Count(),
                    Conversions = conversions,
                    ConversionPercent = g.Count() > 0 ? conversions * 100.0 / g.Count() : 0
                };
            })
            .OrderByDescending(x => x.ConversionPercent)
            .ThenByDescending(x => x.Leads)
            .Take(12)
            .ToList();

        return (dto, null);
    }

    private static List<OrdersByDayRow> BuildOrdersByDay(
        List<Order> ordersCreated,
        DateTime from,
        DateTime to)
    {
        var list = new List<OrdersByDayRow>();
        for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
        {
            var next = day.AddDays(1);
            var dayOrders = ordersCreated
                .Where(o => o.Created_at >= day && o.Created_at < next)
                .ToList();
            var revenue = dayOrders
                .Where(o => o.Delivered_at.HasValue)
                .Sum(o => o.Final_cost > 0 ? o.Final_cost : o.Estimated_cost);
            list.Add(new OrdersByDayRow
            {
                Date = day,
                Count = dayOrders.Count,
                Revenue = revenue
            });
        }

        return list;
    }

    private static List<StatusRow> BuildStatusRows(List<Order> ordersCreated)
    {
        var total = ordersCreated.Count;
        if (total == 0)
            return new List<StatusRow>();

        return ordersCreated
            .GroupBy(o => o.OrderStatus != null ? o.OrderStatus.Name : "Неизвестно")
            .Select(g => new StatusRow
            {
                Status = g.Key,
                Count = g.Count(),
                Share = g.Count() * 100.0 / total
            })
            .OrderByDescending(r => r.Count)
            .ToList();
    }

    private static (DateTime from, DateTime to) NormalizePeriodUtc(DateTime? fromUtc, DateTime? toUtc)
    {
        var now = DateTime.UtcNow;
        var todayUtc = now.Date;

        if (!fromUtc.HasValue && !toUtc.HasValue)
        {
            var monthStart = new DateTime(todayUtc.Year, todayUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfToday = todayUtc.AddDays(1).AddTicks(-1);
            return (monthStart, endOfToday);
        }

        var f = (fromUtc ?? toUtc!.Value).ToUniversalTime().Date;
        var t = (toUtc ?? fromUtc!.Value).ToUniversalTime().Date;
        if (t < f)
            (f, t) = (t, f);

        var from = DateTime.SpecifyKind(f, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(t.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        return (from, to);
    }

    private static bool IsLeadConvertedStage(string? stage)
    {
        if (string.IsNullOrWhiteSpace(stage))
            return false;
        var s = stage.Trim().ToLowerInvariant();
        return s.Contains("договор")
               || s.Contains("сделк")
               || s.Contains("клиент")
               || s.Contains("заказ")
               || s.Contains("успеш");
    }

    private static double EstimateOrderDistanceKm(Order order)
    {
        var pLat = order.PickupAddress?.Latitude;
        var pLon = order.PickupAddress?.Longitude;
        var dLat = order.DeliveryAddress?.Latitude;
        var dLon = order.DeliveryAddress?.Longitude;
        if (!pLat.HasValue || !pLon.HasValue || !dLat.HasValue || !dLon.HasValue)
            return 0;
        return HaversineKm((double)pLat.Value, (double)pLon.Value, (double)dLat.Value, (double)dLon.Value) * 1.18;
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371.0;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private static double DegreesToRadians(double deg) => deg * Math.PI / 180.0;
}
