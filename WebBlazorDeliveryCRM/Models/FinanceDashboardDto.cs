namespace WebBlazorDeliveryCRM.Models;

public sealed class FinanceDashboardDto
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

    public List<OrdersByDayRowDto> OrdersByDay { get; set; } = new();
    public List<StatusRowDto> StatusRows { get; set; } = new();
    public List<CourierRowDto> CourierRows { get; set; } = new();
    public List<ClientRowDto> ClientRows { get; set; } = new();
    public List<ManagerEfficiencyRowDto> ManagerRows { get; set; } = new();
}

public sealed class OrdersByDayRowDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class StatusRowDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Share { get; set; }
}

public sealed class CourierRowDto
{
    public string Name { get; set; } = string.Empty;
    public int Delivered { get; set; }
    public decimal Revenue { get; set; }
    public decimal Rating { get; set; }
}

public sealed class ManagerEfficiencyRowDto
{
    public string Manager { get; set; } = string.Empty;
    public int Leads { get; set; }
    public int Conversions { get; set; }
    public double ConversionPercent { get; set; }
}

public sealed class ClientRowDto
{
    public string Name { get; set; } = string.Empty;
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
}
