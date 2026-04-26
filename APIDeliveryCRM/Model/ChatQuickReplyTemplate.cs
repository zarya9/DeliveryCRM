using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model;

public class ChatQuickReplyTemplate
{
    [Key]
    public int ID_ChatQuickReplyTemplate { get; set; }

    [Required]
    [ForeignKey(nameof(Company))]
    public int Company_id { get; set; }
    public Company Company { get; set; } = null!;

    [Required]
    [ForeignKey(nameof(User))]
    public int User_id { get; set; }
    public User User { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "other";

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public bool Is_active { get; set; } = true;
    public DateTime Created_at { get; set; } = DateTime.UtcNow;
}
