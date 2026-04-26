using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class SubscriptionPlan
    {
        [Key]
        public int ID_SubscriptionPlan { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty; // BASIC, PRO, ENTERPRISE

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public decimal MonthlyPrice { get; set; }
        public int MaxUsers { get; set; }
        public int MaxOrdersPerMonth { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
