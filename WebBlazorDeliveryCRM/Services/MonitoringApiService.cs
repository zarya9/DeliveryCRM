using System.Net.Http.Json;
using System.Text.Json;

namespace WebBlazorDeliveryCRM.Services;

public sealed class MonitoringFeedItemDto
{
    public int ID_OrderTimelineEvent { get; set; }
    public int Order_id { get; set; }
    public int OrderNumber { get; set; }
    public string? EventType { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }
    public DateTime Created_at { get; set; }
}

public sealed class LiveMapMarkerDto
{
    public string Kind { get; set; } = "";
    public int Id { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public bool Online { get; set; }
    public string? Title { get; set; }
}

public sealed class LiveMapDto
{
    public List<LiveMapMarkerDto> Couriers { get; set; } = new();
    public List<LiveMapMarkerDto> Hubs { get; set; } = new();
}

public class MonitoringApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public MonitoringApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<MonitoringFeedItemDto>> GetFeedAsync(int hours = 48, int take = 80)
    {
        var resp = await _http.GetAsync($"/api/Monitoring/feed?hours={hours}&take={take}");
        if (!resp.IsSuccessStatusCode)
            return new List<MonitoringFeedItemDto>();
        await using var stream = await resp.Content.ReadAsStreamAsync();
        var list = await JsonSerializer.DeserializeAsync<List<MonitoringFeedItemDto>>(stream, JsonOpts);
        return list ?? new List<MonitoringFeedItemDto>();
    }

    public async Task<LiveMapDto?> GetLiveMapAsync()
    {
        var resp = await _http.GetAsync("/api/Monitoring/live-map");
        if (!resp.IsSuccessStatusCode)
            return null;
        await using var stream = await resp.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<LiveMapDto>(stream, JsonOpts);
    }
}
