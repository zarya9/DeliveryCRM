using System.Net.Http.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class ServiceAreaZonesApiService
{
    private readonly HttpClient _http;

    public ServiceAreaZonesApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<ServiceAreaZoneDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var list = await _http.GetFromJsonAsync<List<ServiceAreaZoneDto>>("/api/ServiceAreaZones", cancellationToken);
        return list ?? new List<ServiceAreaZoneDto>();
    }

    public async Task<(bool ok, int? id, string? error)> CreateAsync(CreateServiceAreaZoneRequestDto request, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/ServiceAreaZones", request, cancellationToken);
        if (!resp.IsSuccessStatusCode)
            return (false, null, await resp.Content.ReadAsStringAsync(cancellationToken));

        var body = await resp.Content.ReadFromJsonAsync<CreateResponse>(cancellationToken: cancellationToken);
        return (true, body?.Id, null);
    }

    public async Task<bool> AssignCourierAsync(int zoneId, int courierId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"/api/ServiceAreaZones/{zoneId}/assign-courier?courierId={courierId}", null, cancellationToken);
        return resp.IsSuccessStatusCode;
    }

    private sealed class CreateResponse
    {
        public int Id { get; set; }
    }
}
