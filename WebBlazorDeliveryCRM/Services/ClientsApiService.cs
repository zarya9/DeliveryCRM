using System.Net.Http.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class ClientsApiService
{
    private readonly HttpClient _http;

    public ClientsApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<ClientProfileDto?> GetByUserIdAsync(int userId)
    {
        return await _http.GetFromJsonAsync<ClientProfileDto>($"/api/Clients/by-user/{userId}");
    }

    public async Task<ClientProfileDto?> GetProfileAsync(int id)
    {
        return await _http.GetFromJsonAsync<ClientProfileDto>($"/api/Clients/{id}");
    }

    public async Task<List<OrderDto>?> GetOrdersAsync(int clientId)
    {
        return await _http.GetFromJsonAsync<List<OrderDto>>($"/api/Clients/{clientId}/orders");
    }
}
