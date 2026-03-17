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
            return null;
        }

        var meta = await response.Content.ReadFromJsonAsync<LeadMetaDto>(cancellationToken: cancellationToken);
        return meta;
    }

    public async Task<bool> CreateAsync(CreateLeadRequestDto request, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/Leads", request, cancellationToken);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateStageAsync(int leadId, int stageId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"/api/Leads/{leadId}/stage?stageId={stageId}", null, cancellationToken);
        return resp.IsSuccessStatusCode;
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
    }
}

