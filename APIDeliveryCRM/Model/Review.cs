using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class Review
    {
        [Key]
        public int ID_Review { get; set; }

        [Required]
        [ForeignKey(nameof(Company))]
        public int Company_id { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Order))]
        public int Order_id { get; set; }
        public Order Order { get; set; }

        [Required]
        [ForeignKey(nameof(UserAuthor))]
        public int Author_id { get; set; }
        public User UserAuthor { get; set; }

        [Required]
        [ForeignKey(nameof(UserTarget))]
        public int TargetUser_id { get; set; }
        public User UserTarget { get;set; }
        public int Rating { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }

    }
}
