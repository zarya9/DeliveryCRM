using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace APIDeliveryCRM.Hubs;

/// <summary>
/// Трансляция координат курьеров в реальном времени для пользователей той же компании (мониторинг).
/// </summary>
[Authorize]
public class TrackingHub : Hub
{
    public static string CompanyGroup(int companyId) => $"CompanyTracking_{companyId}";

    public override async Task OnConnectedAsync()
    {
        if (TryGetCompanyId(out var companyId))
            await Groups.AddToGroupAsync(Context.ConnectionId, CompanyGroup(companyId));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetCompanyId(out var companyId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, CompanyGroup(companyId));
        await base.OnDisconnectedAsync(exception);
    }

    private bool TryGetCompanyId(out int companyId)
    {
        companyId = default;
        var v = Context.User?.FindFirst("companyId")?.Value;
        return int.TryParse(v, out companyId);
    }
}
