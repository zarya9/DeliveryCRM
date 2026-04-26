namespace WebBlazorDeliveryCRM.Models;

public class CompanySlaSettingsDto
{
    public int CompanyId { get; set; }
    public int SlaOnTimeHours { get; set; } = 4;
    public int SlaLateHours { get; set; } = 24;
}

public class UpdateCompanySlaSettingsDto
{
    public int SlaOnTimeHours { get; set; }
    public int SlaLateHours { get; set; }
}

