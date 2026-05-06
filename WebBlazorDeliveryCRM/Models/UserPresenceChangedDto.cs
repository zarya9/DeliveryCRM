using System.Text.Json.Serialization;

namespace WebBlazorDeliveryCRM.Models;

public class UserPresenceChangedDto
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("online")]
    public bool Online { get; set; }
}
