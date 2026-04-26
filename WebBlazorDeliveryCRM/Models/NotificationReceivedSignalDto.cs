using System.Text.Json.Serialization;

namespace WebBlazorDeliveryCRM.Models;

/// <summary>Push из ChatHub после сохранения уведомления в БД.</summary>
public class NotificationReceivedSignalDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
