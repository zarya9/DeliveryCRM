using System.Text.Json.Serialization;

namespace WebBlazorDeliveryCRM.Models;

public class NotificationReceivedSignalDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
