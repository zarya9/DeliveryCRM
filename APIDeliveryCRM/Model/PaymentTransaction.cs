using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class PaymentTransaction
    {
        [Key]
        public int ID_PaymentTransaction { get; set; }

        [Required]
        [ForeignKey(nameof(Invoice))]
        public int BillingInvoice_id { get; set; }
        public BillingInvoice Invoice { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = "MockPay";

        [Required]
        [MaxLength(100)]
        public string ProviderPaymentId { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "initiated"; // initiated, succeeded, failed

        public decimal Amount { get; set; }
        public DateTime Created_at { get; set; } = DateTime.UtcNow;
        public DateTime? Succeeded_at { get; set; }
        public string? FailureReason { get; set; }
    }
}
