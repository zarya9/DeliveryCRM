using System.Net.Http.Json;
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
}
