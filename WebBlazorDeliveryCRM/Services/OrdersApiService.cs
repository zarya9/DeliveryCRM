using System.Net.Http.Json;
using System.Text.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class OrdersApiService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;

    public OrdersApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<OrderStatusOptionDto>> GetOrderStatusesAsync()
    {
        var resp = await _http.GetAsync("/api/Orders/statuses");
        if (!resp.IsSuccessStatusCode)
            return new List<OrderStatusOptionDto>();
        await using var stream = await resp.Content.ReadAsStreamAsync();
        var list = await JsonSerializer.DeserializeAsync<List<OrderStatusOptionDto>>(stream, JsonOpts);
        return list ?? new List<OrderStatusOptionDto>();
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
        return await GetSafeAsync<List<OrderDto>>(url) ?? new List<OrderDto>();
    }

    public async Task<OrderDto?> GetByIdAsync(int id)
    {
        return await GetSafeAsync<OrderDto>($"/api/Orders/{id}");
    }

    public async Task<List<OrderDto>?> GetByClientAsync(int clientId)
    {
        return await GetSafeAsync<List<OrderDto>>($"/api/Orders/client/{clientId}") ?? new List<OrderDto>();
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
        return await GetSafeAsync<List<OrderTimelineEventDto>>($"/api/Orders/{orderId}/timeline") ?? new List<OrderTimelineEventDto>();
    }

    public async Task<OrderEtaDto?> GetEtaAsync(int orderId)
    {
        return await GetSafeAsync<OrderEtaDto>($"/api/Orders/{orderId}/eta");
    }

    public async Task<(OrderDto? order, string? error)> CreateMineAsync(CreateCustomerOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/Orders/create-mine", request, cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            return (null, string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)resp.StatusCode}" : body);
        }
        var dto = await resp.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: cancellationToken);
        return (dto, null);
    }

    public async Task<(bool ok, string? error)> PayOrderAsync(int orderId)
    {
        var resp = await _http.PostAsync($"/api/Orders/{orderId}/pay", null);
        if (resp.IsSuccessStatusCode)
            return (true, null);
        try
        {
            await using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            if (doc.RootElement.TryGetProperty("message", out var m))
                return (false, m.GetString());
        }
        catch
        {
            /* ignore */
        }

        return (false, $"Ошибка {(int)resp.StatusCode}");
    }

    private async Task<T?> GetSafeAsync<T>(string url)
    {
        var resp = await _http.GetAsync(url);
        if (!resp.IsSuccessStatusCode)
            return default;

        await using var stream = await resp.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOpts);
    }
}
