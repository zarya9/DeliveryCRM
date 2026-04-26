using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class SupportTicket
    {
        [Key]
        public int ID_SupportTicket { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [ForeignKey(nameof(Order))]
        public int? Order_id { get; set; }
        public Order? Order { get; set; }

        [ForeignKey(nameof(ClientProfile))]
        public int? ClientProfile_id { get; set; }
        public ClientProfile? ClientProfile { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Description { get; set; } = string.Empty;

        public SupportTicketCategory Category { get; set; } = SupportTicketCategory.Other;
        public byte Priority { get; set; } = 0;
        public SupportTicketStatus Status { get; set; } = SupportTicketStatus.New;

        [ForeignKey(nameof(ResponsibleUser))]
        public int? ResponsibleUser_id { get; set; }
        public User? ResponsibleUser { get; set; }

        [Required]
        [ForeignKey(nameof(CreatedByUser))]
        public int CreatedByUser_id { get; set; }
        public User CreatedByUser { get; set; } = null!;

        public DateTime Created_at { get; set; } = DateTime.UtcNow;
        public DateTime? FirstResponse_at { get; set; }
        public DateTime? Resolved_at { get; set; }
        public DateTime? Sla_due_at { get; set; }

        [MaxLength(1000)]
        public string? Delay_reason { get; set; }
    }
}
