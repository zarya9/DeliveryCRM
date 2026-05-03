using System.Net.Http.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class ReportsApiService
{
    private readonly HttpClient _http;

    public ReportsApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<FinanceDashboardDto?> GetFinanceDashboardAsync(
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int? companyId = null)
    {
        var qs = new List<string>();
        if (companyId.HasValue)
            qs.Add($"companyId={companyId}");
        if (fromUtc.HasValue)
            qs.Add($"from={Uri.EscapeDataString(fromUtc.Value.ToUniversalTime().ToString("o"))}");
        if (toUtc.HasValue)
            qs.Add($"to={Uri.EscapeDataString(toUtc.Value.ToUniversalTime().ToString("o"))}");
        var query = qs.Count > 0 ? "?" + string.Join("&", qs) : "";
        return await _http.GetFromJsonAsync<FinanceDashboardDto>($"/api/Reports/finance{query}");
    }

    public async Task<byte[]?> GetFinanceExcelAsync(
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int? companyId = null)
    {
        var qs = new List<string>();
        if (companyId.HasValue)
            qs.Add($"companyId={companyId}");
        if (fromUtc.HasValue)
            qs.Add($"from={Uri.EscapeDataString(fromUtc.Value.ToUniversalTime().ToString("o"))}");
        if (toUtc.HasValue)
            qs.Add($"to={Uri.EscapeDataString(toUtc.Value.ToUniversalTime().ToString("o"))}");
        var query = qs.Count > 0 ? "?" + string.Join("&", qs) : "";
        var response = await _http.GetAsync($"/api/Reports/finance/export{query}");
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<byte[]?> GetFinancePdfAsync(
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int? companyId = null)
    {
        var qs = new List<string>();
        if (companyId.HasValue)
            qs.Add($"companyId={companyId}");
        if (fromUtc.HasValue)
            qs.Add($"from={Uri.EscapeDataString(fromUtc.Value.ToUniversalTime().ToString("o"))}");
        if (toUtc.HasValue)
            qs.Add($"to={Uri.EscapeDataString(toUtc.Value.ToUniversalTime().ToString("o"))}");
        var query = qs.Count > 0 ? "?" + string.Join("&", qs) : "";
        var response = await _http.GetAsync($"/api/Reports/finance/export-pdf{query}");
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadAsByteArrayAsync();
    }
}
