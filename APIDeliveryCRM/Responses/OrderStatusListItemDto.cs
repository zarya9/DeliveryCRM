using System.Text.Json.Serialization;

namespace APIDeliveryCRM.Responses;

public sealed class OrderStatusListItemDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
