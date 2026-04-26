namespace WebBlazorDeliveryCRM.Models;

public class CommunicationTemplateDto
{
    public int ID_CommunicationTemplate { get; set; }
    public int Company_id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string TitleTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public int? TriggerStatus_id { get; set; }
    public bool Is_active { get; set; }
}

public class UpsertCommunicationTemplateRequestDto
{
    public string Code { get; set; } = string.Empty;
    public string TitleTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public int? TriggerStatus_id { get; set; }
    public bool Is_active { get; set; } = true;
}
