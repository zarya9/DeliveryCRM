using System.Net.Http.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class GeoAnalyticsApiService
{
    private readonly HttpClient _http;

    public GeoAnalyticsApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<GeoAnalyticsOverviewDto?> GetOverviewAsync(DateTime fromUtc, DateTime toUtc, double gridKm = 3.0, CancellationToken cancellationToken = default)
    {
        var from = Uri.EscapeDataString(fromUtc.ToUniversalTime().ToString("o"));
        var to = Uri.EscapeDataString(toUtc.ToUniversalTime().ToString("o"));
        var url = $"/api/GeoAnalytics/overview?fromUtc={from}&toUtc={to}&gridKm={gridKm:0.##}";
        return await _http.GetFromJsonAsync<GeoAnalyticsOverviewDto>(url, cancellationToken);
    }
}
