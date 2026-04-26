using System.Net.Http.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class AuditApiService
{
    private readonly HttpClient _http;

    public AuditApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<AuditLogRowDto>?> GetLogsAsync(int take = 500)
    {
        return await _http.GetFromJsonAsync<List<AuditLogRowDto>>($"/api/AuditLogs?take={take}");
    }
}
