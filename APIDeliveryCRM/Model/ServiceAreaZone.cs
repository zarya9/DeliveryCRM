using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    /// <summary>Геозона обслуживания компании (упрощенно: круг с центром и радиусом).</summary>
    public class ServiceAreaZone
    {
        [Key]
        public int ID_ServiceAreaZone { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public decimal Center_lat { get; set; }

        [Required]
        public decimal Center_lon { get; set; }

        [Required]
        public decimal Radius_km { get; set; }

        public bool Is_active { get; set; } = true;

        public ICollection<ServiceAreaZoneCourier> Couriers { get; set; } = new List<ServiceAreaZoneCourier>();
    }
}
