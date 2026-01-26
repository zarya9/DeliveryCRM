using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace APIDeliveryCRM.Model
{
    public class CourierStatus
    {
        [Key]
        public int ID_CourierStatus { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [JsonIgnore]
        public ICollection<CourierProfile> CourierProfiles { get; set; } = new List<CourierProfile>();
    }
}
