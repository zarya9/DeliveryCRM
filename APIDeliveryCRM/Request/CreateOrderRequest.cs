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
    }
}

