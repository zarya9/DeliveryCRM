using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace APIDeliveryCRM.Hubs;

/// <summary>
/// Связывает SignalR-соединение с ID пользователя из JWT (для Clients.User).
/// </summary>
public sealed class NameUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var user = connection.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user.FindFirst("sub")?.Value;
    }
}
