using System.Net.Http.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class CommunicationTemplatesApiService
{
    private readonly HttpClient _http;

    public CommunicationTemplatesApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<CommunicationTemplateDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var list = await _http.GetFromJsonAsync<List<CommunicationTemplateDto>>("/api/CommunicationTemplates", cancellationToken);
        return list ?? new List<CommunicationTemplateDto>();
    }

    public async Task<bool> UpsertAsync(UpsertCommunicationTemplateRequestDto req, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/CommunicationTemplates", req, cancellationToken);
        return resp.IsSuccessStatusCode;
    }
}
