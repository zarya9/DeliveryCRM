namespace WebBlazorDeliveryCRM.Models;

public class SupportTicketDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int? OrderId { get; set; }
    public int? ClientProfileId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public byte Priority { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ResponsibleUserId { get; set; }
    public string? ResponsibleUserName { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? SlaDueAt { get; set; }
    public bool IsSlaOverdue { get; set; }
    public string? DelayReason { get; set; }
}

public class CreateSupportTicketRequestDto
{
    public int? Order_id { get; set; }
    public int? ClientProfile_id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public byte Category { get; set; } = 4;
    public byte Priority { get; set; }
    public int? ResponsibleUser_id { get; set; }
}

public class UpdateSupportTicketStatusRequestDto
{
    public byte Status { get; set; }
    public string? DelayReason { get; set; }
}
