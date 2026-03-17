using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class ClientProfile
    {
        [Key]
        public int ID_ClientProfile { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        public string Default_address { get; set; } = string.Empty;
        public decimal Rating { get; set; }

        [ForeignKey(nameof(ClientStatus))]
        public int? ClientStatus_id { get; set; }
        public ClientStatus? ClientStatus { get; set; }

        [ForeignKey(nameof(ClientSegment))]
        public int? ClientSegment_id { get; set; }
        public ClientSegment? ClientSegment { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public int User_id { get; set; }
        public User User { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(PaymentMethod))]
        public int Preferred_payment_method_id { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = null!;
    }
}
