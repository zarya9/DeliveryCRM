using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model;

/// <summary>Одноразовый код сброса пароля (в БД только хэш кода).</summary>
public class PasswordResetCode
{
    [Key]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Login))]
    public int LoginId { get; set; }

    public Login Login { get; set; } = null!;

    [Required]
    [MaxLength(64)]
    public string CodeHash { get; set; } = string.Empty;

    public DateTime ExpiresUtc { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? ConsumedUtc { get; set; }
}
