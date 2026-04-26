using System.Net.Http.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class OrdersApiService
{
    private readonly HttpClient _http;

    public OrdersApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<OrderDto>?> GetAllAsync(int? companyId = null, DateTime? fromUtc = null, DateTime? toUtc = null)
    {
        var qs = new List<string>();
        if (companyId.HasValue)
            qs.Add($"companyId={companyId}");
        if (fromUtc.HasValue)
            qs.Add($"from={Uri.EscapeDataString(fromUtc.Value.ToUniversalTime().ToString("o"))}");
        if (toUtc.HasValue)
            qs.Add($"to={Uri.EscapeDataString(toUtc.Value.ToUniversalTime().ToString("o"))}");
        var url = qs.Count > 0 ? $"/api/Orders?{string.Join("&", qs)}" : "/api/Orders";
        var list = await _http.GetFromJsonAsync<List<OrderDto>>(url);
        return list ?? new List<OrderDto>();
    }

    public async Task<OrderDto?> GetByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<OrderDto>($"/api/Orders/{id}");
    }

    public async Task<List<OrderDto>?> GetByClientAsync(int clientId)
    {
        return await _http.GetFromJsonAsync<List<OrderDto>>($"/api/Orders/client/{clientId}");
    }

    public async Task<bool> AssignCourierAsync(int orderId, int courierId)
    {
        var res = await _http.PostAsync($"/api/Orders/{orderId}/assign?courierId={courierId}", null);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> ManualOverrideAssignAsync(int orderId, int courierId, string? reason = null)
    {
        var suffix = string.IsNullOrWhiteSpace(reason)
            ? string.Empty
            : $"&reason={Uri.EscapeDataString(reason)}";
        var res = await _http.PostAsync($"/api/Orders/{orderId}/assign/override?courierId={courierId}{suffix}", null);
        return res.IsSuccessStatusCode;
    }

    public async Task<OrderDispatchDto?> AutoDispatchAsync(int orderId)
    {
        var resp = await _http.PostAsync($"/api/Orders/{orderId}/auto-dispatch", null);
        if (!resp.IsSuccessStatusCode)
            return null;
        return await resp.Content.ReadFromJsonAsync<OrderDispatchDto>();
    }

    public async Task<bool> ChangeStatusAsync(int orderId, int statusId)
    {
        var res = await _http.PostAsync($"/api/Orders/{orderId}/status?statusId={statusId}", null);
        return res.IsSuccessStatusCode;
    }

    public async Task<List<OrderTimelineEventDto>> GetTimelineAsync(int orderId)
    {
        var list = await _http.GetFromJsonAsync<List<OrderTimelineEventDto>>($"/api/Orders/{orderId}/timeline");
        return list ?? new List<OrderTimelineEventDto>();
    }

    public async Task<OrderEtaDto?> GetEtaAsync(int orderId)
    {
        return await _http.GetFromJsonAsync<OrderEtaDto>($"/api/Orders/{orderId}/eta");
    }
}
