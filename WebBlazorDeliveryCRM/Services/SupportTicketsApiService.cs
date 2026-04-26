using System.Net.Http.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class SupportTicketsApiService
{
    private readonly HttpClient _http;

    public SupportTicketsApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<SupportTicketDto>> GetByCompanyAsync(int companyId, byte? status = null, byte? priority = null, bool onlyOverdue = false, CancellationToken cancellationToken = default)
    {
        var qs = new List<string> { $"companyId={companyId}" };
        if (status.HasValue)
            qs.Add($"status={status.Value}");
        if (priority.HasValue)
            qs.Add($"priority={priority.Value}");
        if (onlyOverdue)
            qs.Add("onlyOverdue=true");

        var url = $"/api/SupportTickets?{string.Join("&", qs)}";
        var list = await _http.GetFromJsonAsync<List<SupportTicketDto>>(url, cancellationToken);
        return list ?? new List<SupportTicketDto>();
    }

    public async Task<(bool ok, string? error)> CreateAsync(int companyId, CreateSupportTicketRequestDto request, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync($"/api/SupportTickets?companyId={companyId}", request, cancellationToken);
        if (resp.IsSuccessStatusCode)
            return (true, null);

        return (false, await resp.Content.ReadAsStringAsync(cancellationToken));
    }

    public async Task<bool> AssignAsync(int ticketId, int responsibleUserId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"/api/SupportTickets/{ticketId}/assign?responsibleUserId={responsibleUserId}", null, cancellationToken);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateStatusAsync(int ticketId, UpdateSupportTicketStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync($"/api/SupportTickets/{ticketId}/status", request, cancellationToken);
        return resp.IsSuccessStatusCode;
    }

    public async Task<SupportTicketAnalyticsDto?> GetAnalyticsAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<SupportTicketAnalyticsDto>($"/api/SupportTickets/analytics?companyId={companyId}", cancellationToken);
    }
}

public class SupportTicketAnalyticsDto
{
    public int Total { get; set; }
    public int Overdue { get; set; }
    public List<CountByNameDto> ByCategory { get; set; } = new();
    public List<CountByNameDto> ByStatus { get; set; } = new();
    public List<ReasonCountDto> TopDelayReasons { get; set; } = new();
}

public class CountByNameDto
{
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ReasonCountDto
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
}
