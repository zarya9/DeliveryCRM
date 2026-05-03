using System.Net.Http.Json;
using System.Text.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class CouriersApiService
{
    private readonly HttpClient _http;

    public CouriersApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<CourierProfileDto>?> GetAllAsync(int? companyId = null)
    {
        var url = companyId.HasValue ? $"/api/Couriers?companyId={companyId}" : "/api/Couriers";
        return await _http.GetFromJsonAsync<List<CourierProfileDto>>(url);
    }

    public async Task<CourierProfileDto?> GetByUserIdAsync(int userId)
    {
        return await _http.GetFromJsonAsync<CourierProfileDto>($"/api/Couriers/by-user/{userId}");
    }

    public async Task<CourierProfileDto?> GetProfileAsync(int id)
    {
        return await _http.GetFromJsonAsync<CourierProfileDto>($"/api/Couriers/{id}");
    }

    public async Task<List<OrderDto>?> GetActiveOrdersAsync(int courierId)
    {
        return await _http.GetFromJsonAsync<List<OrderDto>>($"/api/Couriers/{courierId}/orders");
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

    /// <summary>Назначить ТС курьеру (CurrentCourier_id на стороне API).</summary>
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

    public async Task<(bool ok, string? error)> UpdateLocationAsync(int courierProfileId, decimal lat, decimal lon)
    {
        var url = $"/api/Couriers/{courierProfileId}/location?lat={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        var resp = await _http.PostAsync(url, null);
        if (resp.IsSuccessStatusCode)
            return (true, null);
        var body = await resp.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)resp.StatusCode}" : body);
    }
}
