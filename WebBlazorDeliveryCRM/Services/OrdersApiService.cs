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

    public async Task<List<OrderDto>?> GetAllAsync(int? companyId = null)
    {
        var url = companyId.HasValue ? $"/api/Orders?companyId={companyId}" : "/api/Orders";
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

    public async Task<bool> ChangeStatusAsync(int orderId, int statusId)
    {
        var res = await _http.PostAsync($"/api/Orders/{orderId}/status?statusId={statusId}", null);
        return res.IsSuccessStatusCode;
    }
}
