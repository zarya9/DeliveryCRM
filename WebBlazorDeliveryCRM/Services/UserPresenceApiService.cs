using System.Net.Http.Json;

namespace WebBlazorDeliveryCRM.Services;

public class UserPresenceApiService
{
    private readonly HttpClient _http;

    public UserPresenceApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<HashSet<int>> GetOnlineUserIdsAsync(int companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.GetAsync($"/api/Users/online?companyId={companyId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new HashSet<int>();

            var list = await response.Content.ReadFromJsonAsync<List<int>>(cancellationToken: cancellationToken);
            return list is null ? new HashSet<int>() : new HashSet<int>(list);
        }
        catch
        {
            return new HashSet<int>();
        }
    }
}
