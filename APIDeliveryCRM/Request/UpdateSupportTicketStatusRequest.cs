using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class UpdateSupportTicketStatusRequest
    {
        [Range(1, 5)]
        public byte Status { get; set; }

        [MaxLength(1000)]
        public string? DelayReason { get; set; }
    }
}
