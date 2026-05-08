using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography.Xml;
using System.Text.Json.Serialization;

namespace APIDeliveryCRM.Model
{
    public class User
    {
        [Key]
        public int ID_User {  get; set; }
        public string FName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Patronumic { get; set; }
        public DateTime Created_at { get; set; }
        public bool Is_Active { get; set; }
        
        [MaxLength(50)]
        public string Theme { get; set; } = "light";

        [MaxLength(500)]
        public string? Avatar { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Role))]
        public int Role_id { get; set; }
        public Role Role { get; set; } = null!;

        [JsonIgnore]
        public ICollection<Login> Logins { get; set; } = new List<Login>();
        [JsonIgnore]
        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        [JsonIgnore]
        public ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();
        [JsonIgnore]
        public ICollection<ChatMessage> SentMessages { get; set; } = new List<ChatMessage>();
    }
}
