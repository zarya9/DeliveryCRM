using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class ServiceAreaZoneCourier
    {
        [Key]
        public int ID_ServiceAreaZoneCourier { get; set; }

        [Required]
        [ForeignKey(nameof(Zone))]
        public int ServiceAreaZone_id { get; set; }
        public ServiceAreaZone Zone { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Courier))]
        public int CourierProfile_id { get; set; }
        public CourierProfile Courier { get; set; } = null!;
    }
}
