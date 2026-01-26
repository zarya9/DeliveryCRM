using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class FuelCard
    {
        [Key]
        public int ID_Card { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        public string NumberCard { get; set; } = string.Empty;

        [Required]
        [ForeignKey(nameof(FuelCardType))]
        public int Type_id { get; set; }
        public FuelCardType FuelCardType { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(FuelCardStatus))]
        public int Status_id { get; set; }
        public FuelCardStatus FuelCardStatus { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(FuelCompany))]
        public int FuelCompany_id { get; set; }
        public FuelCompany FuelCompany { get; set; } = null!;
        public int PIN {  get; set; }
        public decimal Balance { get; set; }
        public decimal MonthlyLimit { get; set; }
        public DateTime IssuedDate { get; set; }
        public DateTime IssuedTime { get; set; }
        public bool IsVirtual { get; set; }
        public bool Odometer { get; set; }

    }
}
