using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class BillingInvoice
    {
        [Key]
        public int ID_BillingInvoice { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Plan))]
        public int SubscriptionPlan_id { get; set; }
        public SubscriptionPlan Plan { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Number { get; set; } = string.Empty;

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "pending"; // pending, paid, failed, canceled

        public DateTime Issued_at { get; set; } = DateTime.UtcNow;
        public DateTime Due_at { get; set; }
        public DateTime? Paid_at { get; set; }
        public int PeriodMonths { get; set; } = 1;
    }
}
