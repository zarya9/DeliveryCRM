using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class PaymentMethod
    {
        [Key]
        public int ID_PaymentMethod { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
