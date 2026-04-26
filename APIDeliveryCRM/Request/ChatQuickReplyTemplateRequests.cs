using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Request;

public class UpsertChatQuickReplyTemplateRequest
{
    public int? TemplateId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "other";

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
