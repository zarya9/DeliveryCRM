using System.Net.Http.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class CommunicationTemplatesApiService
{
    private readonly HttpClient _http;
    private readonly AppNotificationService _notify;

    public CommunicationTemplatesApiService(IHttpClientFactory factory, AppNotificationService notify)
    {
        _http = factory.CreateClient("AuthorizedClient");
        _notify = notify;
    }

    public async Task<List<CommunicationTemplateDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<CommunicationTemplateDto>>("/api/CommunicationTemplates", cancellationToken);
            return list ?? new List<CommunicationTemplateDto>();
        }
        catch (HttpRequestException)
        {
            _notify.ShowWarning("API недоступно. Проверьте, что APIDeliveryCRM запущен на порту 5220.");
            return new List<CommunicationTemplateDto>();
        }
        catch (TaskCanceledException)
        {
            _notify.ShowWarning("Таймаут API. Проверьте доступность backend.");
            return new List<CommunicationTemplateDto>();
        }
    }

    public async Task<bool> UpsertAsync(UpsertCommunicationTemplateRequestDto req, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/CommunicationTemplates", req, cancellationToken);
        return resp.IsSuccessStatusCode;
    }
}
