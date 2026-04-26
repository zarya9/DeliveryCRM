using System.Net.Http.Json;
using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public class BillingApiService
{
    private readonly HttpClient _http;

    public BillingApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<SubscriptionPlanDto>> GetPlansAsync(CancellationToken cancellationToken = default)
    {
        var list = await _http.GetFromJsonAsync<List<SubscriptionPlanDto>>("/api/Billing/plans", cancellationToken);
        return list ?? new List<SubscriptionPlanDto>();
    }

    public async Task<CompanySubscriptionDto?> GetSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<CompanySubscriptionDto>("/api/Billing/subscription", cancellationToken);
    }

    public async Task<CheckoutSessionResponseDto?> CreateCheckoutAsync(CreateCheckoutSessionRequestDto req, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/Billing/checkout", req, cancellationToken);
        if (!resp.IsSuccessStatusCode)
            return null;
        return await resp.Content.ReadFromJsonAsync<CheckoutSessionResponseDto>(cancellationToken: cancellationToken);
    }

    public async Task<List<BillingInvoiceDto>> GetInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var list = await _http.GetFromJsonAsync<List<BillingInvoiceDto>>("/api/Billing/invoices", cancellationToken);
        return list ?? new List<BillingInvoiceDto>();
    }
}
