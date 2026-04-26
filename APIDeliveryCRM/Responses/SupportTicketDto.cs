using System;

namespace APIDeliveryCRM.Responses
{
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
}
