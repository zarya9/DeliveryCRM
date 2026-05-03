using System;

namespace APIDeliveryCRM.Responses
{
    public class OrderEtaDto
    {
        public int OrderId { get; set; }
        public DateTime? EtaAtUtc { get; set; }
        public DateTime? SlaDueAtUtc { get; set; }
        public DateTime? DeliveryWindowFromUtc { get; set; }
        public DateTime? DeliveryWindowToUtc { get; set; }
        public string? DeliveryWindowText { get; set; }
        public bool IsSlaBreached { get; set; }
        public bool IsSlaRisk { get; set; }
        public string? DelayReason { get; set; }
    }
}
