using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    [JsonPropertyName("id")]
    public int Id { get; set; }

    public string? Name { get; set; }

    [JsonPropertyName("addressId")]
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

    // Legacy/alternate payload aliases
    [JsonPropertyName("ID_LogisticsHub")]
    public int LegacyHubId
    {
        set
        {
            if (Id <= 0) Id = value;
        }
    }

    [JsonPropertyName("ID_Address")]
    public int LegacyAddressId
    {
        set
        {
            if (AddressId <= 0) AddressId = value;
        }
    }
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

    public async Task<(bool ok, string? error)> CreateAsync(CreateLogisticsHubApiRequest body)
    {
        var res = await _http.PostAsJsonAsync("/api/LogisticsHubs", body);
        if (res.IsSuccessStatusCode)
            return (true, null);
        return (false, await ReadErrorMessageAsync(res));
    }

    public async Task<(bool ok, string? error)> UpdateAsync(int hubId, CreateLogisticsHubApiRequest body)
    {
        var res = await _http.PutAsJsonAsync($"/api/LogisticsHubs/{hubId}", body);
        if (res.IsSuccessStatusCode)
            return (true, null);
        return (false, await ReadErrorMessageAsync(res));
    }

    private static async Task<string?> ReadErrorMessageAsync(HttpResponseMessage res)
    {
        try
        {
            await using var stream = await res.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            if (doc.RootElement.TryGetProperty("message", out var message))
                return message.GetString();
        }
        catch
        {
        }

        return $"Ошибка {(int)res.StatusCode}";
    }

    public async Task<(bool ok, string? error)> DeleteAsync(int hubId)
    {
        var res = await _http.DeleteAsync($"/api/LogisticsHubs/{hubId}");
        if (res.IsSuccessStatusCode)
            return (true, null);
        return (false, await ReadErrorMessageAsync(res));
    }
}
