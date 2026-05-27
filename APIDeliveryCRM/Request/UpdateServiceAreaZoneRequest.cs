using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class UpdateServiceAreaZoneRequest
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public decimal Center_lat { get; set; }

        [Required]
        public decimal Center_lon { get; set; }

        [Range(0.1, 500)]
        public decimal Radius_km { get; set; }

        public bool Is_active { get; set; } = true;
    }
}
