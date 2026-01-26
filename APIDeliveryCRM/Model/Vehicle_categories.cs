using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace APIDeliveryCRM.Model
{
    public class VehicleCategory
    {
        [Key]
        public int ID_Category { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public decimal? Max_Weight { get; set; }
        public decimal? Speed_factor { get; set; }

        [JsonIgnore]
        public ICollection<CourierProfile> CourierProfiles { get; set; } = new List<CourierProfile>();
    }
}
