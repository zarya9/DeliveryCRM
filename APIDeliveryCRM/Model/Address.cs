using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class Address
    {
        [Key]
        public int ID_Address { get; set; }

        [Required]
        [MaxLength(200)]
        public string Street { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string House { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Flat { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? Region { get; set; }

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(User))]
        public int User_id { get; set; }
        public User User { get; set; } = null!;

        public ICollection<Order> PickupOrders { get; set; } = new List<Order>();
        public ICollection<Order> DeliveryOrders { get; set; } = new List<Order>();
    }
}

