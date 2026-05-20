using System.Text.Json.Serialization;

namespace WebBlazorDeliveryCRM.Models;

public sealed class CourierNavRouteResult
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("distanceKm")]
    public double DistanceKm { get; set; }

    [JsonPropertyName("durationMinutes")]
    public int DurationMinutes { get; set; }

    [JsonPropertyName("steps")]
    public List<CourierNavStepDto> Steps { get; set; } = new();
}

public sealed class CourierNavStepDto
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("distanceM")]
    public int DistanceM { get; set; }
}
