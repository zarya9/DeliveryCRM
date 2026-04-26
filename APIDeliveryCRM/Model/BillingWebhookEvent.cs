using System;
using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class BillingWebhookEvent
    {
        [Key]
        public int ID_BillingWebhookEvent { get; set; }

        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string EventKey { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? PaymentId { get; set; }

        [MaxLength(50)]
        public string? EventName { get; set; }

        [MaxLength(50)]
        public string? PaymentStatus { get; set; }

        [MaxLength(4000)]
        public string? RawBody { get; set; }

        public DateTime Processed_at { get; set; } = DateTime.UtcNow;
    }
}
