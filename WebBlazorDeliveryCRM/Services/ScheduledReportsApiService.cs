using System.Net.Http.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class ScheduledReportsApiService
{
    private readonly HttpClient _http;

    public ScheduledReportsApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<ScheduledReportJobDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var list = await _http.GetFromJsonAsync<List<ScheduledReportJobDto>>("/api/ScheduledReports", cancellationToken);
        return list ?? new List<ScheduledReportJobDto>();
    }

    public async Task<bool> UpsertAsync(UpsertScheduledReportJobRequestDto req, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/ScheduledReports", req, cancellationToken);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> RunNowAsync(int id, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"/api/ScheduledReports/{id}/run-now", null, cancellationToken);
        return resp.IsSuccessStatusCode;
    }
}
