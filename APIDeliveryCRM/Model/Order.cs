using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class Order
    {
        [Key]
        public int ID_Order { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public int Order_Number { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(ClientProfile))]
        public int Client_id { get; set; }
        public ClientProfile ClientProfile { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(OrderType))]
        public int OrderType_id { get; set; }
        public OrderType OrderType { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(OrderStatus))]
        public int Status_id { get; set; }
        public OrderStatus OrderStatus { get; set; } = null!;

        [ForeignKey(nameof(CourierProfile))]
        public int? Courier_id { get; set; }
        public CourierProfile? CourierProfile { get; set; }

        [Required]
        [ForeignKey(nameof(PackageType))]
        public int PackageType_id { get; set; }
        public PackageType PackageType { get; set; } = null!;

        public decimal Weight { get; set; }
        public decimal Height { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Estimated_cost { get; set; }
        public decimal Final_cost { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime? Delivered_at { get; set; }
        public DateTime? Pickup_started_at { get; set; }
        public DateTime? In_transit_at { get; set; }
        public DateTime? Arrived_at { get; set; }

        /// <summary>Плановое время доставки (для SLA-контроля).</summary>
        public DateTime? Sla_due_at { get; set; }

        /// <summary>Когда заказ впервые пересек SLA-границу.</summary>
        public DateTime? Sla_breached_at { get; set; }

        /// <summary>ETA в UTC, обновляется при изменении статуса/назначении.</summary>
        public DateTime? Eta_at { get; set; }

        /// <summary>Причина просрочки (ручной ввод менеджера/логиста).</summary>
        [MaxLength(1000)]
        public string? Delay_reason { get; set; }

        /// <summary>Приоритет заказа: 0 обычный, 1 срочный, 2 критический.</summary>
        public byte Priority { get; set; } = 0;

        [Required]
        [ForeignKey(nameof(PaymentMethod))]
        public int PaymentMethod_id { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = null!;
        public bool Is_paid { get; set; }

        [Required]
        [ForeignKey(nameof(PickupAddress))]
        public int PickupAddress_id { get; set; }
        public Address PickupAddress { get; set; }

        [Required]
        [ForeignKey(nameof(DeliveryAddress))]
        public int DeliveryAddress_id { get; set; }
        public Address DeliveryAddress { get; set; }

        /// <summary>Схема доставки: город / через хабы / прямой межгород.</summary>
        public DeliveryRouteKind DeliveryRouteKind { get; set; } = DeliveryRouteKind.LocalUrban;

        [ForeignKey(nameof(OriginHub))]
        public int? OriginHub_id { get; set; }
        public LogisticsHub? OriginHub { get; set; }

        [ForeignKey(nameof(DestinationHub))]
        public int? DestinationHub_id { get; set; }
        public LogisticsHub? DestinationHub { get; set; }

        public ICollection<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>();

        /// <summary>Упорядоченный список точек маршрута (порядок = SortOrder).</summary>
        public ICollection<OrderRouteStop> RouteStops { get; set; } = new List<OrderRouteStop>();
        public ICollection<OrderTimelineEvent> TimelineEvents { get; set; } = new List<OrderTimelineEvent>();
    }
}
