using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class ManagerProfile
    {
        [Key]
        public int ID_ManagerProfile { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(User))]
        public int User_id { get; set; }
        public User User { get; set; } = null!;

        [MaxLength(100)]
        public string? Position { get; set; }

        [MaxLength(50)]
        public string? Department { get; set; }

        [MaxLength(50)]
        public string? Passport_series { get; set; }

        [MaxLength(50)]
        public string? Passport_number { get; set; }

        [MaxLength(200)]
        public string? Passport_issued_by { get; set; }

        public DateTime? Passport_issued_date { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        public DateTime HireDate { get; set; }
        public bool Is_Active { get; set; } = true;
    }
}

