using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillingController : Controller
    {
        private readonly IBillingService _billingService;
        private readonly IConfiguration _configuration;
            private readonly IHostEnvironment _hostEnvironment;

        public BillingController(IBillingService billingService, IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            _billingService = billingService;
            _configuration = configuration;
            _hostEnvironment = hostEnvironment;
        }

        [Authorize(Roles = "Менеджер,Администратор,Админ")]
        [HttpGet("plans")]
        public async Task<IActionResult> Plans()
        {
            return await _billingService.GetPlansAsync();
        }

        [Authorize(Roles = "Менеджер,Администратор,Админ")]
        [HttpGet("subscription")]
        public async Task<IActionResult> MySubscription()
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });
            return await _billingService.GetMySubscriptionAsync(companyId.Value);
        }

        [Authorize(Roles = "Менеджер,Администратор,Админ")]
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CreateCheckoutSessionRequest request)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });
            try
            {
                return await _billingService.CreateCheckoutSessionAsync(companyId.Value, request);
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, new { message = "Платежный провайдер не ответил вовремя. Повторите попытку." });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { message = $"Ошибка связи с платежным провайдером: {ex.Message}" });
            }
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] PaymentWebhookRequest request)
        {
            return await _billingService.HandleWebhookAsync(request);
        }

        [AllowAnonymous]
        [HttpPost("webhook/yookassa")]
        public async Task<IActionResult> YooKassaWebhook([FromBody] JsonElement payload)
        {
            var secret = _configuration["Billing:YooKassa:WebhookSecret"];
            if (_hostEnvironment.IsProduction() && string.IsNullOrWhiteSpace(secret))
                return Unauthorized(new { message = "Webhook secret is not configured for production." });

            if (!string.IsNullOrWhiteSpace(secret))
            {
                var provided = Request.Headers["X-Billing-Webhook-Secret"].FirstOrDefault();
                if (!string.Equals(secret, provided, StringComparison.Ordinal))
                    return Unauthorized(new { message = "Webhook secret mismatch." });
            }

            var allowedIps = _configuration.GetSection("Billing:YooKassa:AllowedIps").Get<string[]>() ?? Array.Empty<string>();
            if (allowedIps.Length > 0)
            {
                var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (string.IsNullOrWhiteSpace(remoteIp) || !allowedIps.Contains(remoteIp))
                    return Unauthorized(new { message = "Webhook IP is not allowed." });
            }

            return await _billingService.HandleYooKassaWebhookAsync(payload);
        }

        [Authorize(Roles = "Менеджер,Администратор,Админ")]
        [HttpGet("invoices")]
        public async Task<IActionResult> Invoices()
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });
            return await _billingService.GetInvoicesAsync(companyId.Value);
        }

        [Authorize(Roles = "Менеджер,Администратор,Админ")]
        [HttpPost("invoices/{invoiceId:int}/pay")]
        public async Task<IActionResult> PayPendingInvoice([FromRoute] int invoiceId)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });
            return await _billingService.PayPendingInvoiceAsync(companyId.Value, invoiceId);
        }

        [Authorize(Roles = "Менеджер,Администратор,Админ")]
        [HttpPost("invoices/{invoiceId:int}/cancel")]
        public async Task<IActionResult> CancelPendingInvoice([FromRoute] int invoiceId)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
                return Unauthorized(new { message = "Не указана компания в токене." });
            return await _billingService.CancelPendingInvoiceAsync(companyId.Value, invoiceId);
        }

        private int? GetCompanyId()
        {
            var raw = User.FindFirst("companyId")?.Value
                      ?? User.FindFirst(ClaimTypes.GroupSid)?.Value;
            return int.TryParse(raw, out var id) ? id : null;
        }
    }
}
