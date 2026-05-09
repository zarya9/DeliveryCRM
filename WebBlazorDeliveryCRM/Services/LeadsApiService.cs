using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WebBlazorDeliveryCRM.Services;

public class LeadsApiService
{
    private readonly HttpClient _http;

    public LeadsApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<LeadDto>> GetByCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var url = $"/api/Leads?companyId={companyId}";
        var response = await _http.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[LeadsApiService] GetByCompanyAsync failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {err}");
            return new List<LeadDto>();
        }

        var list = await response.Content.ReadFromJsonAsync<List<LeadDto>>(cancellationToken: cancellationToken);
        return list ?? new List<LeadDto>();
    }

    public async Task<LeadMetaDto?> GetMetaAsync(CancellationToken cancellationToken = default)
    {
        var url = "/api/Leads/meta";
        var response = await _http.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[LeadsApiService] GetMetaAsync failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {err}");
            return null;
        }

        var meta = await response.Content.ReadFromJsonAsync<LeadMetaDto>(cancellationToken: cancellationToken);
        return meta;
    }

    public async Task<(bool ok, string? error)> CreateAsync(
        CreateLeadRequestDto request,
        int companyId,
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/Leads?companyId={companyId}&managerUserId={managerUserId}";
        var resp = await _http.PostAsJsonAsync(url, request, cancellationToken);
        if (resp.IsSuccessStatusCode)
        {
            return (true, null);
        }

        var errorBody = await resp.Content.ReadAsStringAsync(cancellationToken);
        var error = $"HTTP {(int)resp.StatusCode}: {errorBody}";
        Console.WriteLine($"[LeadsApiService] CreateAsync failed. {error}");
        return (false, error);
    }

    public async Task<(bool ok, string? error)> UpdateAsync(
        int leadId,
        CreateLeadRequestDto request,
        int companyId,
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/Leads/{leadId}?companyId={companyId}&managerUserId={managerUserId}";
        var resp = await _http.PutAsJsonAsync(url, request, cancellationToken);
        if (resp.IsSuccessStatusCode)
            return (true, null);

        var errorBody = await resp.Content.ReadAsStringAsync(cancellationToken);
        var error = $"HTTP {(int)resp.StatusCode}: {errorBody}";
        Console.WriteLine($"[LeadsApiService] UpdateAsync failed. {error}");
        return (false, error);
    }

    public async Task<(bool ok, string? error)> DeleteAsync(int leadId, int companyId, CancellationToken cancellationToken = default)
    {
        var url = $"/api/Leads/{leadId}?companyId={companyId}";
        var resp = await _http.DeleteAsync(url, cancellationToken);
        if (resp.IsSuccessStatusCode)
            return (true, null);

        var errorBody = await resp.Content.ReadAsStringAsync(cancellationToken);
        var error = $"HTTP {(int)resp.StatusCode}: {errorBody}";
        Console.WriteLine($"[LeadsApiService] DeleteAsync failed. {error}");
        return (false, error);
    }

    public async Task<bool> UpdateStageAsync(int leadId, int stageId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"/api/Leads/{leadId}/stage?stageId={stageId}", null, cancellationToken);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> MarkLostAsync(int leadId, string reason, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"/api/Leads/{leadId}/lost?reason={Uri.EscapeDataString(reason)}", null, cancellationToken);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> MarkWonAsync(int leadId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"/api/Leads/{leadId}/won", null, cancellationToken);
        return resp.IsSuccessStatusCode;
    }

    public async Task<LeadAnalyticsDto?> GetAnalyticsAsync(int companyId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var qs = new List<string> { $"companyId={companyId}" };
        if (from.HasValue)
            qs.Add($"from={Uri.EscapeDataString(from.Value.ToUniversalTime().ToString("o"))}");
        if (to.HasValue)
            qs.Add($"to={Uri.EscapeDataString(to.Value.ToUniversalTime().ToString("o"))}");
        var resp = await _http.GetAsync($"/api/Leads/analytics?{string.Join("&", qs)}", cancellationToken);
        if (!resp.IsSuccessStatusCode)
            return null;
        return await resp.Content.ReadFromJsonAsync<LeadAnalyticsDto>(cancellationToken: cancellationToken);
    }

    public sealed class LeadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Contact { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public int? ManagerUserId { get; set; }
        public string? ManagerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Comment { get; set; }
        public string? LostReason { get; set; }
        public DateTime? WonAt { get; set; }
        public DateTime? LostAt { get; set; }
        public string? NextTaskTitle { get; set; }
        public DateTime? NextTaskDueAtUtc { get; set; }
    }

    public sealed class LeadMetaDto
    {
        public List<IdNameDto> Sources { get; set; } = new();
        public List<IdNameDto> Stages { get; set; } = new();
    }

    public sealed class IdNameDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed class CreateLeadRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Contact { get; set; }
        public int LeadSourceId { get; set; }
        public int LeadStageId { get; set; }
        public string? Comment { get; set; }
        public string? NextTaskTitle { get; set; }
        public DateTime? NextTaskDueAtUtc { get; set; }
    }

    public sealed class LeadAnalyticsDto
    {
        public int Total { get; set; }
        public int Won { get; set; }
        public int Lost { get; set; }
        public double ConversionPercent { get; set; }
        public List<FunnelItemDto> Funnel { get; set; } = new();
        public List<LostReasonItemDto> TopLostReasons { get; set; } = new();
        public List<UpcomingTaskItemDto> UpcomingTasks { get; set; } = new();
    }

    public sealed class FunnelItemDto
    {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public sealed class LostReasonItemDto
    {
        public string Reason { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public sealed class UpcomingTaskItemDto
    {
        public int LeadId { get; set; }
        public string LeadName { get; set; } = string.Empty;
        public string? TaskTitle { get; set; }
        public DateTime? DueAt { get; set; }
    }
}

