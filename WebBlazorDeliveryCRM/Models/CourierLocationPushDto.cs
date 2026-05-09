using System.Text.Json.Serialization;

namespace WebBlazorDeliveryCRM.Models;

/// <summary>
/// Полезная нагрузка события SignalR CourierLocationUpdated.
/// </summary>
public sealed class CourierLocationPushDto
{
    [JsonPropertyName("courierProfileId")]
    public int CourierProfileId { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public bool Online { get; set; }
    public string? Title { get; set; }
}
