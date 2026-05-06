using System.Text.Json;

namespace WebBlazorDeliveryCRM.Services;

public sealed class AddressSuggestItemDto
{
    public string? DisplayName { get; set; }
    public string? Street { get; set; }
    public string? House { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PrimaryLine { get; set; }
    public string? SecondaryLine { get; set; }
    public bool HasHouse { get; set; }
    public string? Lat { get; set; }
    public string? Lon { get; set; }
}

public class AddressSuggestApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AddressSuggestApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<AddressSuggestItemDto>> SuggestAsync(string query, int limit = 7, string? city = null)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            return new List<AddressSuggestItemDto>();
        var trimmedQuery = query.Trim();
        var cappedLimit = Math.Clamp(limit, 1, 10);

        async Task<List<AddressSuggestItemDto>> CallAsync(string? cityFilter)
        {
            var url = $"/api/AddressSuggest?q={Uri.EscapeDataString(trimmedQuery)}&limit={cappedLimit}";
            if (!string.IsNullOrWhiteSpace(cityFilter))
                url += $"&city={Uri.EscapeDataString(cityFilter.Trim())}";
            var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                return new List<AddressSuggestItemDto>();
            await using var stream = await resp.Content.ReadAsStreamAsync();
            var list = await JsonSerializer.DeserializeAsync<List<AddressSuggestItemDto>>(stream, JsonOpts);
            return list ?? new List<AddressSuggestItemDto>();
        }

        var byCity = await CallAsync(city);
        if (byCity.Count > 0 || string.IsNullOrWhiteSpace(city))
            return byCity;

        return await CallAsync(null);
    }
}
