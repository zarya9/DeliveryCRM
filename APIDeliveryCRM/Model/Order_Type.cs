using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class OrderType
    {
        [Key]
        public int ID_OrderType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Base_price { get; set; }

        public decimal Price_km { get; set; }
        public decimal Estimated_delivery_factor { get; set; }
    }
}
