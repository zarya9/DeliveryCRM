using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebBlazorDeliveryCRM.Services;

public class HubOrderOnSiteDto
{
    public int OrderId { get; set; }
    public int OrderNumber { get; set; }
    public string? ClientName { get; set; }
    public string? DeliveryTo { get; set; }
}

public class LogisticsHubListItemDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int AddressId { get; set; }
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? House { get; set; }
    public string? Flat { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public List<HubOrderOnSiteDto> OrdersOnSite { get; set; } = new();
}

public class CreateLogisticsHubApiRequest
{
    public string Name { get; set; } = "";
    public string Street { get; set; } = "";
    public string House { get; set; } = "";
    public string? Flat { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string? Comment { get; set; }
}

public class LogisticsHubsApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public LogisticsHubsApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<LogisticsHubListItemDto>> GetMineAsync()
    {
        var stream = await _http.GetStreamAsync("/api/LogisticsHubs");
        var list = await JsonSerializer.DeserializeAsync<List<LogisticsHubListItemDto>>(stream, JsonOptions);
        return list ?? new List<LogisticsHubListItemDto>();
    }

    public async Task<bool> CreateAsync(CreateLogisticsHubApiRequest body)
    {
        var res = await _http.PostAsJsonAsync("/api/LogisticsHubs", body);
        return res.IsSuccessStatusCode;
    }
}
