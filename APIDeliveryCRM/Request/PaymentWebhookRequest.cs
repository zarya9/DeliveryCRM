using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class PaymentWebhookRequest
    {
        [Required]
        public string ProviderPaymentId { get; set; } = string.Empty;

        [Required]
        public string Event { get; set; } = string.Empty; // payment.succeeded | payment.failed

        public string? FailureReason { get; set; }
    }
}
