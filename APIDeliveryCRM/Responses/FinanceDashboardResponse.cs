using System;
using System.Collections.Generic;

namespace APIDeliveryCRM.Responses;

public sealed class FinanceDashboardResponse
{
    public int SlaOnTimeHours { get; set; } = 4;
    public int SlaLateHours { get; set; } = 24;

    public DateTime PeriodFromUtc { get; set; }
    public DateTime PeriodToUtc { get; set; }

    public int OrdersCreatedInPeriod { get; set; }
    public decimal RevenueDeliveredInPeriod { get; set; }
    public decimal AvgCheckDeliveredInPeriod { get; set; }
    public int PaidDeliveredCount { get; set; }
    public decimal PaidSharePercent { get; set; }
    public double AvgDeliveryHours { get; set; }
    public double OnTimePercent { get; set; }
    public double LatePercent { get; set; }
    public double FuelConsumptionLitersPer100Km { get; set; } = 10.0;
    public double EstimatedFuelUsedLiters { get; set; }
    public double EstimatedFuelSavedLiters { get; set; }
    public double EstimatedFuelSavingsPercent { get; set; }

    public List<OrdersByDayRow> OrdersByDay { get; set; } = new();
    public List<StatusRow> StatusRows { get; set; } = new();
    public List<CourierRow> CourierRows { get; set; } = new();
    public List<ClientRow> ClientRows { get; set; } = new();
    public List<ManagerEfficiencyRow> ManagerRows { get; set; } = new();
}

public sealed class OrdersByDayRow
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class StatusRow
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Share { get; set; }
}

public sealed class CourierRow
{
    public string Name { get; set; } = string.Empty;
    public int Delivered { get; set; }
    public decimal Revenue { get; set; }
    public decimal Rating { get; set; }
}

public sealed class ManagerEfficiencyRow
{
    public string Manager { get; set; } = string.Empty;
    public int Leads { get; set; }
    public int Conversions { get; set; }
    public double ConversionPercent { get; set; }
}

public sealed class ClientRow
{
    public string Name { get; set; } = string.Empty;
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
}
