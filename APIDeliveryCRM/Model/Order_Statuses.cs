using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class OrderStatus
    {
        [Key]
        public int ID_OrderStatus { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
