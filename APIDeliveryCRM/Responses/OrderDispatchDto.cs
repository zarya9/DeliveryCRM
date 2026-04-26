using System;

namespace APIDeliveryCRM.Responses
{
    public class OrderDispatchDto
    {
        public int OrderId { get; set; }
        public int CourierId { get; set; }
        public decimal? DistanceKm { get; set; }
        public int ActiveOrders { get; set; }
        public bool IsSlaRisk { get; set; }
        public DateTime? EtaAt { get; set; }
        public string DecisionReason { get; set; } = string.Empty;
    }
}
