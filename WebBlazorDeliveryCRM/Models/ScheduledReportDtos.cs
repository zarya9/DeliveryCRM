namespace WebBlazorDeliveryCRM.Models;

public class ScheduledReportJobDto
{
    public int ID_ScheduledReportJob { get; set; }
    public int Company_id { get; set; }
    public string ReportType { get; set; } = "FINANCE";
    public string Frequency { get; set; } = "Daily";
    public string TimeUtc { get; set; } = "06:00";
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public bool Is_active { get; set; }
    public DateTime? LastRun_at { get; set; }
    public DateTime NextRun_at { get; set; }
}

public class UpsertScheduledReportJobRequestDto
{
    public int? JobId { get; set; }
    public string ReportType { get; set; } = "FINANCE";
    public string Frequency { get; set; } = "Daily";
    public string TimeUtc { get; set; } = "06:00";
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public bool Is_active { get; set; } = true;
}
