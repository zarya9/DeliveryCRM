using System.Text.Json.Serialization;

namespace WebBlazorDeliveryCRM.Models;

/// <summary>Событие SignalR ChatHub: смена онлайн-статуса сотрудника в компании.</summary>
public class UserPresenceChangedDto
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("online")]
    public bool Online { get; set; }
}
