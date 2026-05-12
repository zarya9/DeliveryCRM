using System.Net.Http.Json;
using System.Text.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class CompaniesApiService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;

    public CompaniesApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<CompanyOrderTargetDto>> GetForCustomerOrderAsync(CancellationToken cancellationToken = default)
    {
        var resp = await _http.GetAsync("/api/Companies/for-customer-order", cancellationToken);
        if (!resp.IsSuccessStatusCode)
            return new List<CompanyOrderTargetDto>();
        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
        var list = await JsonSerializer.DeserializeAsync<List<CompanyOrderTargetDto>>(stream, JsonOpts, cancellationToken);
        return list ?? new List<CompanyOrderTargetDto>();
    }
}
