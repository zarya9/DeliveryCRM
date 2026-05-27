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

    public async Task<ShiftPlanSummaryDto?> GetCourierPlanAsync(int courierProfileId)
        => await GetSafeAsync<ShiftPlanSummaryDto>($"/api/ShiftPlanner/courier/{courierProfileId}");

    public async Task<(ShiftPlanSummaryDto? plan, string? error)> ApplyCourierRouteAsync(
        int courierProfileId,
        IReadOnlyList<ApplyCourierRouteStopDto> stops)
    {
        var resp = await _http.PostAsJsonAsync(
            $"/api/ShiftPlanner/courier/{courierProfileId}/apply-route",
            new ApplyCourierRouteRequestDto { Stops = stops.ToList() });

        if (!resp.IsSuccessStatusCode)
        {
            var error = await TryReadErrorMessageAsync(resp);
            return (null, error ?? $"Ошибка API ({(int)resp.StatusCode}).");
        }

        await using var stream = await resp.Content.ReadAsStreamAsync();
        var plan = await JsonSerializer.DeserializeAsync<ShiftPlanSummaryDto>(stream, JsonOpts);
        return (plan, plan == null ? "Пустой ответ сервера." : null);
    }

    private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage resp)
    {
        try
        {
            await using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                return msg.GetString();
        }
        catch
        {
            // ignore
        }

        return null;
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
