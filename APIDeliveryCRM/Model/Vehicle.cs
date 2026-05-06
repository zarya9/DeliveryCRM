using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class Vehicle
    {
        [Key]
        public int ID_Vehicle { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        public string License_plate { get; set; } //РіРѕСЃРЅРѕРјРµСЂ
        public string VIN {  get; set; }

        [Required]
        [ForeignKey(nameof(VehicleCategory))]
        public int Category_id { get; set; }
        public VehicleCategory VehicleCategory { get; set; } = null!;

        [ForeignKey(nameof(VehicleModel))]
        public int? Model_id { get; set; }
        public VehicleModel? VehicleModel { get; set; }

        public string Brand_name { get; set; } = string.Empty;

        public string Model_name { get; set; } = string.Empty;

        public DateOnly Year { get; set; }
        public string Color { get; set; }

        [Required]
        [ForeignKey(nameof(VehicleBodyType))]
        public int BodyType_id { get; set; }
        public VehicleBodyType VehicleBodyType { get; set; } = null!;

        public decimal Cargo_volume { get; set; }
        public decimal Max_cargo_weight { get; set; }

        [Required]
        [ForeignKey(nameof(FuelType))]
        public int FuelType_id { get; set; }
        public FuelType FuelType { get; set; } = null!;

        public decimal FuelTank_Capacity { get; set; }
        public decimal Current_mileage { get; set; }
        public string Insurance_policy{ get; set; }
        public DateTime? Insurance_expires_at { get; set; }
        public DateTime? Registration_expires_at { get; set; }
        public DateTime? Maintenance_due_at { get; set; }
        public bool Is_available { get; set; } = true;

        [ForeignKey(nameof(CourierProfile))]
        public int? CurrentCourier_id { get; set; }
        public CourierProfile? CourierProfile { get; set; }
    }
}
