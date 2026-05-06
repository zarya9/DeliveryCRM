using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using APIDeliveryCRM.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace APIDeliveryCRM.Services
{
    public class BillingService : IBillingService
    {
        private readonly ContextDB _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public BillingService(ContextDB context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> GetPlansAsync()
        {
            await EnsureDefaultPlansAsync();
            var plans = await _context.SubscriptionPlans
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.MonthlyPrice)
                .Select(p => new SubscriptionPlanDto
                {
                    Id = p.ID_SubscriptionPlan,
                    Code = p.Code,
                    Name = p.Name,
                    MonthlyPrice = p.MonthlyPrice,
                    MaxUsers = p.MaxUsers,
                    MaxOrdersPerMonth = p.MaxOrdersPerMonth
                })
                .ToListAsync();

            return new OkObjectResult(plans);
        }

        public async Task<IActionResult> GetMySubscriptionAsync(int companyId)
        {
            var subscription = await EnsureSubscriptionAsync(companyId);
            if (subscription == null)
                return new NotFoundObjectResult(new { message = "Не найдена подписка компании." });

            await ApplyScheduledDowngradeIfDueAsync(subscription);
            return new OkObjectResult(await MapSubscriptionAsync(subscription));
        }

        public async Task<IActionResult> CreateCheckoutSessionAsync(int companyId, CreateCheckoutSessionRequest request)
        {
            await EnsureDefaultPlansAsync();
            if (string.IsNullOrWhiteSpace(request.PlanCode))
                return new BadRequestObjectResult(new { message = "Не указан код тарифа." });
            var planCode = request.PlanCode.Trim().ToUpperInvariant();
            var plan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Code == planCode && p.IsActive);
            if (plan == null)
                return new BadRequestObjectResult(new { message = "Тариф не найден." });

            var subscription = await EnsureSubscriptionAsync(companyId);
            if (subscription == null)
                return new BadRequestObjectResult(new { message = "Подписка компании не инициализирована." });

            await CleanupDuplicateSubscriptionsAsync(companyId, subscription.ID_CompanySubscription);
            await ApplyScheduledDowngradeIfDueAsync(subscription);

            var hasPendingPayment = await _context.BillingInvoices.AsNoTracking()
                .AnyAsync(i => i.Company_id == companyId && (i.Status == "pending" || i.Status == "pending_upgrade"));
            if (hasPendingPayment)
                return new ConflictObjectResult(new { message = "У вас уже есть ожидающий оплату счёт. Завершите его или дождитесь отмены." });

            if (string.Equals(subscription.Plan.Code, plan.Code, StringComparison.OrdinalIgnoreCase))
                return new BadRequestObjectResult(new { message = "Этот тариф уже активен у компании." });

            var isUpgrade = plan.MonthlyPrice > subscription.Plan.MonthlyPrice;
            if (!isUpgrade)
            {
                var existingScheduled = await _context.BillingInvoices.AsNoTracking()
                    .AnyAsync(i => i.Company_id == companyId && i.Status == "scheduled_downgrade");
                if (existingScheduled)
                    return new ConflictObjectResult(new { message = "Понижение уже запланировано на следующий период." });

                var scheduledInvoice = new BillingInvoice
                {
                    Company_id = companyId,
                    SubscriptionPlan_id = plan.ID_SubscriptionPlan,
                    Number = $"SCH-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                    Amount = 0m,
                    Currency = "RUB",
                    Status = "scheduled_downgrade",
                    Issued_at = DateTime.UtcNow,
                    Due_at = subscription.CurrentPeriodEnd_at,
                    PeriodMonths = 1
                };
                _context.BillingInvoices.Add(scheduledInvoice);
                await _context.SaveChangesAsync();

                return new OkObjectResult(new CheckoutSessionResponse
                {
                    InvoiceId = scheduledInvoice.ID_BillingInvoice,
                    InvoiceNumber = scheduledInvoice.Number,
                    ProviderPaymentId = string.Empty,
                    CheckoutUrl = string.Empty,
                    Amount = 0m,
                    Currency = "RUB",
                    Action = "scheduled_downgrade",
                    Message = $"Понижение до тарифа {plan.Name} запланировано на {subscription.CurrentPeriodEnd_at:dd.MM.yyyy}."
                });
            }

            var amount = CalculateProratedUpgradeAmount(subscription, plan);
            if (amount <= 0m)
            {
                subscription.SubscriptionPlan_id = plan.ID_SubscriptionPlan;
                subscription.Status = "active";
                subscription.Canceled_at = null;
                await _context.SaveChangesAsync();
                await SyncCompanyPlanAsync(companyId, plan, subscription.CurrentPeriodEnd_at);

                return new OkObjectResult(new CheckoutSessionResponse
                {
                    InvoiceId = 0,
                    InvoiceNumber = string.Empty,
                    ProviderPaymentId = string.Empty,
                    CheckoutUrl = string.Empty,
                    Amount = 0m,
                    Currency = "RUB",
                    Action = "upgraded",
                    Message = $"Тариф повышен до {plan.Name}. Доплата не требуется."
                });
            }

            var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

            var invoice = new BillingInvoice
            {
                Company_id = companyId,
                SubscriptionPlan_id = plan.ID_SubscriptionPlan,
                Number = invoiceNumber,
                Amount = amount,
                Currency = "RUB",
                Status = "pending_upgrade",
                Issued_at = DateTime.UtcNow,
                Due_at = DateTime.UtcNow.AddDays(3),
                PeriodMonths = 1
            };
            _context.BillingInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            var provider = GetPaymentProvider();
            var providerPaymentId = $"MOCK-{Guid.NewGuid():N}".ToUpperInvariant();
            var checkoutUrl = $"https://mockpay.local/checkout/{providerPaymentId}";

            if (provider == "YooKassa")
            {
                var createResult = await CreateYooKassaPaymentAsync(invoice);
                if (!createResult.ok)
                    return new BadRequestObjectResult(new { message = createResult.error ?? "Не удалось создать платеж в YooKassa." });

                providerPaymentId = createResult.providerPaymentId!;
                checkoutUrl = createResult.checkoutUrl!;
            }

            var tx = new PaymentTransaction
            {
                BillingInvoice_id = invoice.ID_BillingInvoice,
                Provider = provider,
                ProviderPaymentId = providerPaymentId,
                Status = "initiated",
                Amount = amount
            };
            _context.PaymentTransactions.Add(tx);
            await _context.SaveChangesAsync();

            var checkout = new CheckoutSessionResponse
            {
                InvoiceId = invoice.ID_BillingInvoice,
                InvoiceNumber = invoice.Number,
                ProviderPaymentId = tx.ProviderPaymentId,
                CheckoutUrl = checkoutUrl,
                Amount = amount,
                Currency = invoice.Currency,
                Action = "checkout",
                Message = $"Доплата за повышение тарифа до {plan.Name}."
            };
            return new OkObjectResult(checkout);
        }

        public async Task<IActionResult> HandleWebhookAsync(PaymentWebhookRequest request)
        {
            var tx = await _context.PaymentTransactions
                .Include(t => t.Invoice)
                .ThenInclude(i => i.Company)
                .Include(t => t.Invoice)
                .ThenInclude(i => i.Plan)
                .FirstOrDefaultAsync(t => t.ProviderPaymentId == request.ProviderPaymentId);

            if (tx == null)
                return new NotFoundObjectResult(new { message = "Транзакция не найдена." });

            if (request.Event == "payment.succeeded")
            {
                if (tx.Status != "succeeded")
                {
                    tx.Status = "succeeded";
                    tx.Succeeded_at = DateTime.UtcNow;
                    var invoiceStatusBeforePay = tx.Invoice.Status;
                    tx.Invoice.Status = "paid";
                    tx.Invoice.Paid_at = DateTime.UtcNow;

                    var subscription = await EnsureSubscriptionAsync(tx.Invoice.Company_id);
                    if (subscription == null)
                        return new BadRequestObjectResult(new { message = "Подписка компании не инициализирована." });

                    var now = DateTime.UtcNow;
                    var isUpgradeInvoice = string.Equals(invoiceStatusBeforePay, "pending_upgrade", StringComparison.OrdinalIgnoreCase);
                    subscription.SubscriptionPlan_id = tx.Invoice.SubscriptionPlan_id;
                    subscription.Status = "active";
                    subscription.Canceled_at = null;
                    if (!isUpgradeInvoice)
                    {
                        var periodStart = subscription.CurrentPeriodEnd_at > now ? subscription.CurrentPeriodEnd_at : now;
                        subscription.CurrentPeriodStart_at = periodStart;
                        subscription.CurrentPeriodEnd_at = periodStart.AddMonths(tx.Invoice.PeriodMonths);
                    }

                    tx.Invoice.Company.SubscriptionPlan = tx.Invoice.Plan.Code;
                    tx.Invoice.Company.MaxUsers = tx.Invoice.Plan.MaxUsers;
                    tx.Invoice.Company.MaxOrdersPerMonth = tx.Invoice.Plan.MaxOrdersPerMonth;
                    tx.Invoice.Company.SubscriptionExpiresAt = subscription.CurrentPeriodEnd_at;
                    tx.Invoice.Company.Is_Active = true;
                }
            }
            else if (request.Event == "payment.failed")
            {
                tx.Status = "failed";
                tx.FailureReason = string.IsNullOrWhiteSpace(request.FailureReason) ? "Payment failed by provider." : request.FailureReason.Trim();
                tx.Invoice.Status = "failed";

                var subscription = await EnsureSubscriptionAsync(tx.Invoice.Company_id);
                if (subscription != null && subscription.CurrentPeriodEnd_at <= DateTime.UtcNow)
                    subscription.Status = "past_due";
            }
            else
            {
                return new BadRequestObjectResult(new { message = "Неподдерживаемое событие вебхука." });
            }

            await _context.SaveChangesAsync();
            return new OkObjectResult(new { message = "Webhook processed." });
        }

        public async Task<IActionResult> HandleYooKassaWebhookAsync(JsonElement payload)
        {
            try
            {
                var eventName = payload.GetProperty("event").GetString();
                var obj = payload.GetProperty("object");
                var paymentId = obj.GetProperty("id").GetString();
                var status = obj.GetProperty("status").GetString();
                var eventKey = $"{eventName}:{paymentId}:{status}";

                if (string.IsNullOrWhiteSpace(paymentId))
                    return new BadRequestObjectResult(new { message = "Missing payment id." });

                var alreadyProcessed = await _context.BillingWebhookEvents
                    .AsNoTracking()
                    .AnyAsync(e => e.Provider == "YooKassa" && e.EventKey == eventKey);
                if (alreadyProcessed)
                    return new OkObjectResult(new { message = "Duplicate webhook ignored." });

                IActionResult result;
                if (eventName == "payment.succeeded" || status == "succeeded")
                {
                    result = await HandleWebhookAsync(new PaymentWebhookRequest
                    {
                        ProviderPaymentId = paymentId!,
                        Event = "payment.succeeded"
                    });
                }
                else if (eventName == "payment.canceled" || status == "canceled")
                {
                    result = await HandleWebhookAsync(new PaymentWebhookRequest
                    {
                        ProviderPaymentId = paymentId!,
                        Event = "payment.failed",
                        FailureReason = "Canceled in YooKassa"
                    });
                }
                else
                {
                    result = new OkObjectResult(new { message = "Webhook ignored." });
                }

                var raw = payload.GetRawText();
                _context.BillingWebhookEvents.Add(new BillingWebhookEvent
                {
                    Provider = "YooKassa",
                    EventKey = eventKey,
                    PaymentId = paymentId,
                    EventName = eventName,
                    PaymentStatus = status,
                    RawBody = raw.Length <= 4000 ? raw : raw[..4000]
                });
                await _context.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { message = $"Invalid YooKassa payload: {ex.Message}" });
            }
        }

        public async Task<IActionResult> GetInvoicesAsync(int companyId)
        {
            var items = await _context.BillingInvoices
                .AsNoTracking()
                .Where(i => i.Company_id == companyId)
                .OrderByDescending(i => i.Issued_at)
                .Take(200)
                .Select(i => new
                {
                    id = i.ID_BillingInvoice,
                    number = i.Number,
                    planCode = i.Plan.Code,
                    planName = i.Plan.Name,
                    amount = i.Amount,
                    currency = i.Currency,
                    status = i.Status,
                    issuedAt = i.Issued_at,
                    dueAt = i.Due_at,
                    paidAt = i.Paid_at,
                    periodMonths = i.PeriodMonths,
                    description = i.Status == "scheduled_downgrade" ? "Запланированное понижение тарифа" : null
                })
                .ToListAsync();

            return new OkObjectResult(items);
        }

        private async Task<CompanySubscription?> EnsureSubscriptionAsync(int companyId)
        {
            await EnsureDefaultPlansAsync();
            var sub = await _context.CompanySubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Company_id == companyId);
            if (sub != null)
            {
                await CleanupDuplicateSubscriptionsAsync(companyId, sub.ID_CompanySubscription);
                await ApplyScheduledDowngradeIfDueAsync(sub);
                return sub;
            }

            var basicPlan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Code == "BASIC" && p.IsActive);
            if (basicPlan == null)
                return null;

            sub = new CompanySubscription
            {
                Company_id = companyId,
                SubscriptionPlan_id = basicPlan.ID_SubscriptionPlan,
                Status = "trialing",
                Started_at = DateTime.UtcNow,
                CurrentPeriodStart_at = DateTime.UtcNow,
                CurrentPeriodEnd_at = DateTime.UtcNow.AddDays(14),
                AutoRenew = true
            };
            _context.CompanySubscriptions.Add(sub);
            await _context.SaveChangesAsync();

            return await _context.CompanySubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.ID_CompanySubscription == sub.ID_CompanySubscription);
        }

        private async Task<CompanySubscriptionDto> MapSubscriptionAsync(CompanySubscription s)
        {
            var pending = await _context.BillingInvoices
                .AsNoTracking()
                .Include(i => i.Plan)
                .Where(i => i.Company_id == s.Company_id && i.Status == "scheduled_downgrade")
                .OrderBy(i => i.Due_at)
                .FirstOrDefaultAsync();

            return new CompanySubscriptionDto
            {
                CompanyId = s.Company_id,
                Status = s.Status,
                PlanCode = s.Plan.Code,
                PlanName = s.Plan.Name,
                CurrentPeriodEndAt = s.CurrentPeriodEnd_at,
                AutoRenew = s.AutoRenew,
                PendingPlanCode = pending?.Plan?.Code,
                PendingPlanName = pending?.Plan?.Name,
                PendingPlanEffectiveAt = pending?.Due_at
            };
        }

        private async Task CleanupDuplicateSubscriptionsAsync(int companyId, int keepSubscriptionId)
        {
            var duplicateSubs = await _context.CompanySubscriptions
                .Where(s => s.Company_id == companyId && s.ID_CompanySubscription != keepSubscriptionId)
                .ToListAsync();
            if (duplicateSubs.Count == 0)
                return;

            _context.CompanySubscriptions.RemoveRange(duplicateSubs);
            await _context.SaveChangesAsync();
        }

        private async Task ApplyScheduledDowngradeIfDueAsync(CompanySubscription subscription)
        {
            var scheduled = await _context.BillingInvoices
                .Include(i => i.Plan)
                .Where(i => i.Company_id == subscription.Company_id && i.Status == "scheduled_downgrade")
                .OrderBy(i => i.Due_at)
                .FirstOrDefaultAsync();
            if (scheduled == null || scheduled.Due_at > DateTime.UtcNow)
                return;

            subscription.SubscriptionPlan_id = scheduled.SubscriptionPlan_id;
            subscription.Status = "active";
            subscription.CurrentPeriodStart_at = scheduled.Due_at;
            subscription.CurrentPeriodEnd_at = scheduled.Due_at.AddMonths(1);
            scheduled.Status = "applied";
            scheduled.Paid_at = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _context.Entry(subscription).Reference(x => x.Plan).LoadAsync();
            await SyncCompanyPlanAsync(subscription.Company_id, scheduled.Plan, subscription.CurrentPeriodEnd_at);
        }

        private decimal CalculateProratedUpgradeAmount(CompanySubscription subscription, SubscriptionPlan targetPlan)
        {
            var cycleSeconds = (subscription.CurrentPeriodEnd_at - subscription.CurrentPeriodStart_at).TotalSeconds;
            if (cycleSeconds <= 0)
                cycleSeconds = TimeSpan.FromDays(30).TotalSeconds;
            var remainingSeconds = Math.Max(0d, (subscription.CurrentPeriodEnd_at - DateTime.UtcNow).TotalSeconds);
            var ratio = Math.Min(1d, remainingSeconds / cycleSeconds);
            var current = subscription.Plan.MonthlyPrice;
            var target = targetPlan.MonthlyPrice;
            var due = (target - current) * (decimal)ratio;
            return Math.Round(Math.Max(0m, due), 2, MidpointRounding.AwayFromZero);
        }

        private async Task SyncCompanyPlanAsync(int companyId, SubscriptionPlan plan, DateTime periodEnd)
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.ID_Company == companyId);
            if (company == null)
                return;
            company.SubscriptionPlan = plan.Code;
            company.MaxUsers = plan.MaxUsers;
            company.MaxOrdersPerMonth = plan.MaxOrdersPerMonth;
            company.SubscriptionExpiresAt = periodEnd;
            company.Is_Active = true;
            await _context.SaveChangesAsync();
        }

        private async Task EnsureDefaultPlansAsync()
        {
            if (await _context.SubscriptionPlans.AnyAsync())
                return;

            _context.SubscriptionPlans.AddRange(
                new SubscriptionPlan
                {
                    Code = "BASIC",
                    Name = "Basic",
                    MonthlyPrice = 1990m,
                    MaxUsers = 10,
                    MaxOrdersPerMonth = 1000,
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Code = "PRO",
                    Name = "Pro",
                    MonthlyPrice = 4990m,
                    MaxUsers = 50,
                    MaxOrdersPerMonth = 10000,
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Code = "ENTERPRISE",
                    Name = "Enterprise",
                    MonthlyPrice = 14990m,
                    MaxUsers = 500,
                    MaxOrdersPerMonth = 200000,
                    IsActive = true
                });
            await _context.SaveChangesAsync();
        }

        private string GetPaymentProvider()
        {
            var provider = _configuration["Billing:Provider"]?.Trim();
            return string.IsNullOrWhiteSpace(provider) ? "MockPay" : provider;
        }

        private async Task<(bool ok, string? providerPaymentId, string? checkoutUrl, string? error)> CreateYooKassaPaymentAsync(BillingInvoice invoice)
        {
            var shopId = _configuration["Billing:YooKassa:ShopId"];
            var secret = _configuration["Billing:YooKassa:SecretKey"];
            var returnUrl = _configuration["Billing:YooKassa:ReturnUrl"] ?? "http://localhost:5000";
            var currency = _configuration["Billing:YooKassa:Currency"] ?? "RUB";

            if (string.IsNullOrWhiteSpace(shopId) || string.IsNullOrWhiteSpace(secret))
                return (false, null, null, "YooKassa credentials are not configured.");

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://api.yookassa.ru/v3/");
            client.Timeout = TimeSpan.FromSeconds(20);
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shopId}:{secret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
            client.DefaultRequestHeaders.Add("Idempotence-Key", Guid.NewGuid().ToString("N"));

            var body = new
            {
                amount = new { value = invoice.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), currency },
                capture = true,
                confirmation = new { type = "redirect", return_url = returnUrl },
                description = $"Invoice {invoice.Number}",
                metadata = new
                {
                    invoiceId = invoice.ID_BillingInvoice,
                    companyId = invoice.Company_id
                }
            };

            var resp = await client.PostAsJsonAsync("payments", body);
            if (!resp.IsSuccessStatusCode)
            {
                var errBody = await resp.Content.ReadAsStringAsync();
                return (false, null, null, $"YooKassa error: {(int)resp.StatusCode} {errBody}");
            }

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            var id = root.GetProperty("id").GetString();
            string? url = null;
            if (root.TryGetProperty("confirmation", out var confirmation) &&
                confirmation.TryGetProperty("confirmation_url", out var u))
            {
                url = u.GetString();
            }

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(url))
                return (false, null, null, "YooKassa returned invalid payment response.");

            return (true, id, url, null);
        }
    }
}
