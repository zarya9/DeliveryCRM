using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class VehicleModel
    {
        [Key]
        public int ID_Model { get; set; }

        [Required]
        [ForeignKey(nameof(VehicleBrand))]
        public int Brand_id { get; set; }
        public VehicleBrand VehicleBrand { get; set; } = null!;
        public string Name { get; set; }
        public DateOnly Year { get; set; }
        public decimal AvgFuelCity { get; set; }
        public decimal AvgFuelHighWay { get; set; }
        public decimal EngineCapacity { get; set; }
        public int HorsePower { get; set; }
        [Required]
        [ForeignKey(nameof(TransmissionType))] 
        public int TransmissionType_id { get; set; }
        public TransmissionType TransmissionType { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(VehicleDriveType))]
        public int DriveType_id { get; set; }
        public VehicleDriveType VehicleDriveType { get; set; } = null!;
    }
}
