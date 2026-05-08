using System.Net.Http.Json;
using System.Text.Json;
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

    public async Task<BillingCheckoutResultDto> CreateCheckoutAsync(CreateCheckoutSessionRequestDto req, CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(25));
            var resp = await _http.PostAsJsonAsync("/api/Billing/checkout", req, cts.Token);
            if (!resp.IsSuccessStatusCode)
            {
                var errorBody = await resp.Content.ReadAsStringAsync(cts.Token);
                var error = string.IsNullOrWhiteSpace(errorBody)
                    ? $"Ошибка оплаты (HTTP {(int)resp.StatusCode})."
                    : ExtractErrorMessage(errorBody);
                return new BillingCheckoutResultDto { Ok = false, Error = error };
            }

            var payload = await resp.Content.ReadFromJsonAsync<CheckoutSessionResponseDto>(cancellationToken: cts.Token);
            if (payload is null)
                return new BillingCheckoutResultDto { Ok = false, Error = "Сервер вернул пустой ответ оплаты." };
            return new BillingCheckoutResultDto { Ok = true, Session = payload };
        }
        catch (TaskCanceledException)
        {
            return new BillingCheckoutResultDto { Ok = false, Error = "Платежный сервис не ответил вовремя. Попробуйте снова." };
        }
        catch (Exception ex)
        {
            return new BillingCheckoutResultDto { Ok = false, Error = $"Не удалось начать оплату: {ex.Message}" };
        }
    }

    public async Task<List<BillingInvoiceDto>> GetInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var list = await _http.GetFromJsonAsync<List<BillingInvoiceDto>>("/api/Billing/invoices", cancellationToken);
        return list ?? new List<BillingInvoiceDto>();
    }

    public async Task<string?> PayPendingInvoiceAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"/api/Billing/invoices/{invoiceId}/pay", content: null, cancellationToken);
        return await HandleActionResponseAsync(resp, cancellationToken);
    }

    public async Task<string?> CancelPendingInvoiceAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"/api/Billing/invoices/{invoiceId}/cancel", content: null, cancellationToken);
        return await HandleActionResponseAsync(resp, cancellationToken);
    }

    private static async Task<string?> HandleActionResponseAsync(HttpResponseMessage resp, CancellationToken cancellationToken)
    {
        var body = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (resp.IsSuccessStatusCode)
            return null;
        return string.IsNullOrWhiteSpace(body)
            ? $"Операция не выполнена (HTTP {(int)resp.StatusCode})."
            : ExtractErrorMessage(body);
    }

    private static string ExtractErrorMessage(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "Неизвестная ошибка.";

        var text = raw.Trim();
        if (!text.StartsWith("{", StringComparison.Ordinal))
            return text;

        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
            {
                var value = message.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value!;
            }
        }
        catch
        {
        }

        return text;
    }
}
