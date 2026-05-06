using System.Text.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class ShiftPlannerApiService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;

    public ShiftPlannerApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<CompanyPlannerResultDto?> GetCurrentAsync(int? companyId = null)
    {
        var url = companyId.HasValue ? $"/api/ShiftPlanner?companyId={companyId.Value}" : "/api/ShiftPlanner";
        return await GetSafeAsync<CompanyPlannerResultDto>(url);
    }

    public async Task<CompanyPlannerResultDto?> RebuildAsync(int? companyId = null, string? reason = null)
    {
        var query = new List<string>();
        if (companyId.HasValue) query.Add($"companyId={companyId.Value}");
        if (!string.IsNullOrWhiteSpace(reason)) query.Add($"reason={Uri.EscapeDataString(reason.Trim())}");
        var suffix = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        var resp = await _http.PostAsync("/api/ShiftPlanner/rebuild" + suffix, null);
        if (!resp.IsSuccessStatusCode)
            return null;
        await using var stream = await resp.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<CompanyPlannerResultDto>(stream, JsonOpts);
    }

    private async Task<T?> GetSafeAsync<T>(string url)
    {
        var resp = await _http.GetAsync(url);
        if (!resp.IsSuccessStatusCode)
            return default;
        await using var stream = await resp.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOpts);
    }
}
