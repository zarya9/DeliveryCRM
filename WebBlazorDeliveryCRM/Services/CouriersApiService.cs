using System.Net.Http.Json;
using System.Text.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class CouriersApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions RouteMapJson = new() { PropertyNameCaseInsensitive = true };

    public CouriersApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<CourierProfileDto>?> GetAllAsync(int? companyId = null)
    {
        var url = companyId.HasValue ? $"/api/Couriers?companyId={companyId}" : "/api/Couriers";
        return await GetSafeAsync<List<CourierProfileDto>>(url) ?? new List<CourierProfileDto>();
    }

    public async Task<CourierProfileDto?> GetByUserIdAsync(int userId)
    {
        return await GetSafeAsync<CourierProfileDto>($"/api/Couriers/by-user/{userId}");
    }

    public async Task<CourierProfileDto?> GetProfileAsync(int id)
    {
        return await GetSafeAsync<CourierProfileDto>($"/api/Couriers/{id}");
    }

    public async Task<List<OrderDto>?> GetActiveOrdersAsync(int courierId)
    {
        return await GetSafeAsync<List<OrderDto>>($"/api/Couriers/{courierId}/orders") ?? new List<OrderDto>();
    }

    public async Task<ShiftClosureSummaryDto?> GetShiftSummaryAsync(int shiftId)
        => await GetSafeAsync<ShiftClosureSummaryDto>($"/api/Couriers/shift/{shiftId}/summary");

    public async Task<CourierRouteMapDto?> GetRouteMapAsync(int courierId)
    {
        var resp = await _http.GetAsync($"/api/Couriers/{courierId}/route-map");
        if (!resp.IsSuccessStatusCode)
            return null;
        await using var stream = await resp.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<CourierRouteMapDto>(stream, RouteMapJson);
    }

    public async Task<List<VehicleDto>?> GetVehiclesByCompanyAsync(int companyId)
    {
        try
        {
            var resp = await _http.GetAsync($"/api/Couriers/vehicles?companyId={companyId}");
            if (!resp.IsSuccessStatusCode)
                return null;
            return await resp.Content.ReadFromJsonAsync<List<VehicleDto>>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool ok, string? error)> AssignVehicleAsync(int courierProfileId, int vehicleId)
    {
        var resp = await _http.PostAsync($"/api/Couriers/{courierProfileId}/assign-vehicle?vehicleId={vehicleId}", null);
        if (resp.IsSuccessStatusCode)
            return (true, null);
        var body = await resp.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)resp.StatusCode}" : body);
    }

    public async Task<(bool ok, string? error)> UpdateDocumentsAsync(int courierProfileId, string? driverLicense, string? passportData)
    {
        var resp = await _http.PutAsJsonAsync($"/api/Couriers/{courierProfileId}/documents", new
        {
            driverLicense,
            passportData
        });
        if (resp.IsSuccessStatusCode)
            return (true, null);
        var body = await resp.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)resp.StatusCode}" : body);
    }

    public async Task<(bool active, int? shiftId, DateTime? timeStart)> GetActiveShiftAsync(int courierProfileId)
    {
        var resp = await _http.GetAsync($"/api/Couriers/{courierProfileId}/shift/active");
        if (!resp.IsSuccessStatusCode)
            return (false, null, null);
        await using var stream = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;
        if (!root.TryGetProperty("active", out var aEl) || !aEl.GetBoolean())
            return (false, null, null);
        int? sid = null;
        if (root.TryGetProperty("shiftId", out var idEl) && idEl.TryGetInt32(out var s))
            sid = s;
        DateTime? ts = null;
        if (root.TryGetProperty("timeStart", out var tEl) && tEl.TryGetDateTime(out var dt))
            ts = dt;
        return (true, sid, ts);
    }

    public async Task<(bool ok, int? shiftId, string? error)> StartShiftAsync(int courierProfileId)
    {
        var resp = await _http.PostAsync($"/api/Couriers/{courierProfileId}/shift/start", null);
        if (!resp.IsSuccessStatusCode)
        {
            var errBody = await resp.Content.ReadAsStringAsync();
            return (false, null, string.IsNullOrWhiteSpace(errBody) ? $"HTTP {(int)resp.StatusCode}" : errBody);
        }

        await using var stream = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        if (doc.RootElement.TryGetProperty("shiftId", out var idEl) && idEl.TryGetInt32(out var sid))
            return (true, sid, null);
        return (true, null, null);
    }

    public async Task<(bool ok, string? error)> EndShiftAsync(int shiftId)
    {
        var resp = await _http.PostAsync($"/api/Couriers/shift/{shiftId}/end", null);
        if (resp.IsSuccessStatusCode)
            return (true, null);
        var body = await resp.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)resp.StatusCode}" : body);
    }

    public async Task<(bool ok, string? error)> SetOnlineAsync(int courierProfileId, bool isOnline)
    {
        var resp = await _http.PostAsync($"/api/Couriers/{courierProfileId}/online?isOnline={(isOnline ? "true" : "false")}", null);
        if (resp.IsSuccessStatusCode)
            return (true, null);
        var body = await resp.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)resp.StatusCode}" : body);
    }

    public async Task<(bool ok, string? error, IReadOnlyList<NearbyDeliveryStopDto> nearby)> UpdateLocationAsync(
        int courierProfileId,
        decimal lat,
        decimal lon)
    {
        var url = $"/api/Couriers/{courierProfileId}/location?lat={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        var resp = await _http.PostAsync(url, null);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)resp.StatusCode}" : body, Array.Empty<NearbyDeliveryStopDto>());
        }

        var nearby = await ParseNearbyStopsAsync(resp);
        return (true, null, nearby);
    }

    public async Task<IReadOnlyList<NearbyDeliveryStopDto>> GetNearbyStopsAsync(int courierProfileId, decimal lat, decimal lon)
    {
        var url = $"/api/Couriers/{courierProfileId}/nearby-stops?lat={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        var resp = await _http.GetAsync(url);
        if (!resp.IsSuccessStatusCode)
            return Array.Empty<NearbyDeliveryStopDto>();
        return await ParseNearbyStopsAsync(resp);
    }

    private static async Task<IReadOnlyList<NearbyDeliveryStopDto>> ParseNearbyStopsAsync(HttpResponseMessage resp)
    {
        await using var stream = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        if (!doc.RootElement.TryGetProperty("nearbyStops", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return Array.Empty<NearbyDeliveryStopDto>();

        var list = new List<NearbyDeliveryStopDto>();
        foreach (var item in arr.EnumerateArray())
        {
            list.Add(new NearbyDeliveryStopDto
            {
                AssignmentId = item.TryGetProperty("assignmentId", out var a) ? a.GetInt32() : 0,
                OrderId = item.TryGetProperty("orderId", out var o) ? o.GetInt32() : 0,
                OrderNumber = item.TryGetProperty("orderNumber", out var n) ? n.GetInt32() : 0,
                Title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                AddressLine = item.TryGetProperty("addressLine", out var ad) ? ad.GetString() ?? "" : "",
                DistanceMeters = item.TryGetProperty("distanceMeters", out var d) ? d.GetDouble() : 0
            });
        }

        return list;
    }

    private async Task<T?> GetSafeAsync<T>(string url)
    {
        var resp = await _http.GetAsync(url);
        if (!resp.IsSuccessStatusCode)
            return default;
        await using var stream = await resp.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<T>(stream, RouteMapJson);
    }
}
