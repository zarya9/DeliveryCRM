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
}
