using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class OrderTimelineEvent
    {
        [Key]
        public int ID_OrderTimelineEvent { get; set; }

        [Required]
        [ForeignKey(nameof(Order))]
        public int Order_id { get; set; }
        public Order Order { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Title { get; set; }

        [MaxLength(1500)]
        public string? Message { get; set; }

        public int? OldStatus_id { get; set; }
        public int? NewStatus_id { get; set; }

        public int? OldCourier_id { get; set; }
        public int? NewCourier_id { get; set; }

        public int? ActorUser_id { get; set; }

        public DateTime Created_at { get; set; } = DateTime.UtcNow;
    }
}
