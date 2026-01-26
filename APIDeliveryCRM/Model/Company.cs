using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIDeliveryCRM.Model
{
    public class Company
    {
        [Key]
        public int ID_Company { get; set; }

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

        public DateTime Created_at { get; set; }
        public bool Is_Active { get; set; } = true;

        [MaxLength(50)]
        public string SubscriptionPlan { get; set; } = "Basic";

        public int MaxUsers { get; set; } = 10;
        public int MaxOrdersPerMonth { get; set; } = 1000;
        public DateTime SubscriptionExpiresAt { get; set; }

        [MaxLength(500)]
        public string? AzureStorageConnectionString { get; set; }

        [MaxLength(100)]
        public string? AzureStorageContainerName { get; set; }

        [MaxLength(500)]
        public string? KafkaBootstrapServers { get; set; }

        [MaxLength(100)]
        public string? KafkaGroupId { get; set; }

        [JsonIgnore]
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}

