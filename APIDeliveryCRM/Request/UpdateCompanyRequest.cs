using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request
{
    public class UpdateCompanyRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Subdomain { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(50)]
        public string? PrimaryColor { get; set; }

        [MaxLength(50)]
        public string? SecondaryColor { get; set; }

        [MaxLength(50)]
        public string? SubscriptionPlan { get; set; }

        [Range(1, 100000)]
        public int? MaxUsers { get; set; }

        [Range(1, 10000000)]
        public int? MaxOrdersPerMonth { get; set; }
    }
}
