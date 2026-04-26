using System.Net.Http.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class CompanySettingsApiService
{
    private readonly HttpClient _http;

    public CompanySettingsApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<CompanySlaSettingsDto?> GetSlaAsync(int? companyId = null)
    {
        var url = companyId.HasValue ? $"/api/CompanySettings/sla?companyId={companyId}" : "/api/CompanySettings/sla";
        return await _http.GetFromJsonAsync<CompanySlaSettingsDto>(url);
    }

    public async Task<CompanySlaSettingsDto?> UpdateSlaAsync(UpdateCompanySlaSettingsDto request, int? companyId = null)
    {
        var url = companyId.HasValue ? $"/api/CompanySettings/sla?companyId={companyId}" : "/api/CompanySettings/sla";
        var response = await _http.PutAsJsonAsync(url, request);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<CompanySlaSettingsDto>();
    }
}

