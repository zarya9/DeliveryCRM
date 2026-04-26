using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace APIDeliveryCRM.Interfaces
{
    public interface IBillingService
    {
        Task<IActionResult> GetPlansAsync();
        Task<IActionResult> GetMySubscriptionAsync(int companyId);
        Task<IActionResult> CreateCheckoutSessionAsync(int companyId, CreateCheckoutSessionRequest request);
        Task<IActionResult> HandleWebhookAsync(PaymentWebhookRequest request);
        Task<IActionResult> HandleYooKassaWebhookAsync(JsonElement payload);
        Task<IActionResult> GetInvoicesAsync(int companyId);
    }
}
