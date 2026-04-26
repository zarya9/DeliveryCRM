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
            result = result.Replace("{orderNumber}", order.Order_Number.ToString(), StringComparison.OrdinalIgnoreCase);
            result = result.Replace("{statusName}", statusName ?? $"#{order.Status_id}", StringComparison.OrdinalIgnoreCase);
            result = result.Replace("{eta}", order.Eta_at?.ToString("dd.MM.yyyy HH:mm") ?? "не рассчитано", StringComparison.OrdinalIgnoreCase);
            result = result.Replace("{delayReason}", order.Delay_reason ?? "-", StringComparison.OrdinalIgnoreCase);
            result = result.Replace("{priority}", order.Priority.ToString(), StringComparison.OrdinalIgnoreCase);
            return result;
        }
    }
}
