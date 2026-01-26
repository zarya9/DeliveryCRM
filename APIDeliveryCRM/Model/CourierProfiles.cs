using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIDeliveryCRM.Model
{
    public class CourierProfile
    {
        [Key]
        public int ID_CourierProfile { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(User))]
        public int User_id { get; set; }
        public User User { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(VehicleCategory))]
        public int VehicleCategory_id { get; set; }
        public VehicleCategory VehicleCategory { get; set; } = null!;

        public string? DriverLicense { get; set; }
        public string? Passport_data { get; set; }

        [Required]
        [ForeignKey(nameof(ScheduleType))]
        public int WorkSchedule_id { get; set; }
        public ScheduleType ScheduleType { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(CourierStatus))]
        public int CurrentStatus_id { get; set; }
        public CourierStatus CourierStatus { get; set; } = null!;

        public decimal Rating { get; set; }
        public int Total_deliveries { get; set; }
        public bool Is_online { get; set; }
        public decimal Current_lat  { get; set; }
        public decimal Current_lon { get; set; }
        public DateTime LastActivity_at { get; set; }

    }
}
