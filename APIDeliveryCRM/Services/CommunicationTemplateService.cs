using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class CommunicationTemplateService : ICommunicationTemplateService
    {
        private readonly ContextDB _context;

        public CommunicationTemplateService(ContextDB context)
        {
            _context = context;
        }

        public async Task<IActionResult> GetByCompanyAsync(int companyId)
        {
            var list = await _context.CommunicationTemplates
                .AsNoTracking()
                .Where(t => t.Company_id == companyId)
                .OrderBy(t => t.Code)
                .ToListAsync();
            return new OkObjectResult(list);
        }

        public async Task<IActionResult> UpsertAsync(int companyId, UpsertCommunicationTemplateRequest request)
        {
            var code = request.Code.Trim().ToUpperInvariant();
            var existing = await _context.CommunicationTemplates
                .FirstOrDefaultAsync(t => t.Company_id == companyId && t.Code == code);

            if (existing == null)
            {
                existing = new CommunicationTemplate
                {
                    Company_id = companyId,
                    Code = code
                };
                _context.CommunicationTemplates.Add(existing);
            }

            existing.TitleTemplate = request.TitleTemplate.Trim();
            existing.BodyTemplate = request.BodyTemplate.Trim();
            existing.TriggerStatus_id = request.TriggerStatus_id;
            existing.Is_active = request.Is_active;

            await _context.SaveChangesAsync();
            return new OkObjectResult(new { id = existing.ID_CommunicationTemplate });
        }

        public async Task<CommunicationTemplate?> ResolveForOrderStatusAsync(int companyId, int statusId)
        {
            var exact = await _context.CommunicationTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Company_id == companyId && t.Is_active && t.TriggerStatus_id == statusId);
            if (exact != null)
                return exact;

            return await _context.CommunicationTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Company_id == companyId && t.Is_active && t.Code == "ORDER_STATUS_CHANGED");
        }

        public string Render(string template, Order order, string? statusName)
        {
            var result = template;
            var windowText = BuildDeliveryWindowText(order.Eta_at, order.Sla_due_at);
            result = result.Replace("{orderNumber}", order.Order_Number.ToString(), StringComparison.OrdinalIgnoreCase);
            result = result.Replace("{statusName}", statusName ?? $"#{order.Status_id}", StringComparison.OrdinalIgnoreCase);
            result = result.Replace("{eta}", order.Eta_at?.ToString("dd.MM.yyyy HH:mm") ?? "не рассчитано", StringComparison.OrdinalIgnoreCase);
            result = result.Replace("{delayReason}", order.Delay_reason ?? "-", StringComparison.OrdinalIgnoreCase);
            result = result.Replace("{priority}", order.Priority.ToString(), StringComparison.OrdinalIgnoreCase);
            result = result.Replace("{deliveryWindow}", windowText, StringComparison.OrdinalIgnoreCase);
            result = result.Replace("{estimatedCost}", order.Estimated_cost.ToString("0.##"), StringComparison.OrdinalIgnoreCase);
            return result;
        }

        private static string BuildDeliveryWindowText(DateTime? fromUtc, DateTime? toUtc)
        {
            if (!fromUtc.HasValue && !toUtc.HasValue)
                return "дата уточняется";
            if (!fromUtc.HasValue)
                return $"до {FormatDayMonth(toUtc!.Value)}";
            if (!toUtc.HasValue)
                return $"с {FormatDayMonth(fromUtc.Value)}";

            var from = fromUtc.Value;
            var to = toUtc.Value;
            if (from.Date == to.Date)
                return FormatDayMonth(from);
            if (from.Month == to.Month && from.Year == to.Year)
                return $"с {from:dd} по {to:dd} {MonthRu(to.Month)}";
            return $"с {FormatDayMonth(from)} по {FormatDayMonth(to)}";
        }

        private static string FormatDayMonth(DateTime dt) => $"{dt:dd} {MonthRu(dt.Month)}";

        private static string MonthRu(int month) => month switch
        {
            1 => "января",
            2 => "февраля",
            3 => "марта",
            4 => "апреля",
            5 => "мая",
            6 => "июня",
            7 => "июля",
            8 => "августа",
            9 => "сентября",
            10 => "октября",
            11 => "ноября",
            12 => "декабря",
            _ => string.Empty
        };
    }
}
