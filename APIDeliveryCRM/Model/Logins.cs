using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class Login
    {
        [Key]
        public int ID_Login { get; set; }

        [Required]
        [MaxLength(256)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [ForeignKey(nameof(User))]
        public int ID_User { get; set; }
        public User User { get; set; } = null!;
    }
}
