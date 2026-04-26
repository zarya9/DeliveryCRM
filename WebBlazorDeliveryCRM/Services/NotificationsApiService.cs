using System.Net.Http.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class NotificationsApiService
{
    private readonly HttpClient _http;

    public NotificationsApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<NotificationItemDto>?> GetMineAsync(bool onlyCritical = false, bool onlyUnread = false, byte? minPriority = null, bool onlyRequiresAck = false, CancellationToken cancellationToken = default)
    {
        var qs = new List<string>();
        if (onlyCritical) qs.Add("onlyCritical=true");
        if (onlyUnread) qs.Add("onlyUnread=true");
        if (minPriority.HasValue) qs.Add($"minPriority={minPriority.Value}");
        if (onlyRequiresAck) qs.Add("onlyRequiresAck=true");
        var url = qs.Count == 0 ? "/api/Notifications/me" : $"/api/Notifications/me?{string.Join("&", qs)}";
        return await _http.GetFromJsonAsync<List<NotificationItemDto>>(url, cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"/api/Notifications/{id}/read", null, cancellationToken);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> AcknowledgeAsync(int id, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"/api/Notifications/{id}/ack", null, cancellationToken);
        return resp.IsSuccessStatusCode;
    }
}
