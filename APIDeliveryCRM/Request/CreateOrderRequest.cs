using System;
using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class CreateOrderRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;

        [Required]
        public int Client_id { get; set; }

        [Required]
        public int OrderType_id { get; set; }

        [Required]
        public int Status_id { get; set; }

        public int? Courier_id { get; set; }

        [Required]
        public int PackageType_id { get; set; }

        [Required]
        public decimal Weight { get; set; }

        [Required]
        public decimal Height { get; set; }

        [Required]
        public decimal Length { get; set; }

        [Required]
        public decimal Width { get; set; }

        public decimal Estimated_cost { get; set; }

        [Required]
        public int PaymentMethod_id { get; set; }

        [Required]
        public int PickupAddress_id { get; set; }

        [Required]
        public int DeliveryAddress_id { get; set; }

        public byte DeliveryRouteKind { get; set; } = 1;

        public DateTime? RequestedDeliveryAtUtc { get; set; }

        [Range(0, 3)]
        public byte Priority { get; set; } = 0;

        public int? OriginHub_id { get; set; }
        public int? DestinationHub_id { get; set; }

        public bool AutoSelectRouteKind { get; set; } = true;
    }
}

