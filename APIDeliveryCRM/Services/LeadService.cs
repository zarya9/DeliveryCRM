using System;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class LeadService : ILeadService
    {
        private readonly ContextDB _context;

        public LeadService(ContextDB context)
        {
            _context = context;
        }

        public async Task<IActionResult> GetByCompanyAsync(int companyId)
        {
            var leads = await _context.Leads
                .Include(l => l.Source)
                .Include(l => l.Stage)
                .Include(l => l.Manager)
                .Where(l => l.Company_id == companyId)
                .OrderByDescending(l => l.Created_at)
                .ToListAsync();

            var dto = leads.Select(l => new LeadDto
            {
                Id = l.ID_Lead,
                Name = l.Name,
                Contact = l.Contact,
                Source = l.Source.Name,
                Stage = l.Stage.Name,
                ManagerUserId = l.ManagerUser_id,
                ManagerName = l.Manager != null ? l.Manager.FName + " " + l.Manager.Name : null,
                CreatedAt = l.Created_at,
                Comment = l.Comment,
                LostReason = l.Lost_reason,
                WonAt = l.Won_at,
                LostAt = l.Lost_at,
                NextTaskTitle = l.NextTask_title,
                NextTaskDueAtUtc = l.NextTask_due_at
            }).ToList();

            return new OkObjectResult(dto);
        }

        public async Task<IActionResult> GetMetaAsync()
        {
            var sources = await _context.LeadSources
                .OrderBy(s => s.Name)
                .ToListAsync();
            var stages = await _context.LeadStages
                .OrderBy(s => s.SortOrder)
                .ToListAsync();

            return new OkObjectResult(new
            {
                sources = sources.Select(s => new { id = s.ID_LeadSource, name = s.Name }),
                stages = stages.Select(s => new { id = s.ID_LeadStage, name = s.Name })
            });
        }

        public async Task<IActionResult> CreateAsync(CreateLeadRequest request, int companyId, int managerUserId)
        {
            var source = await _context.LeadSources.FindAsync(request.LeadSourceId);
            if (source == null)
            {
                return new BadRequestObjectResult(new { message = "Источник лида не найден" });
            }

            var stage = await _context.LeadStages.FindAsync(request.LeadStageId);
            if (stage == null)
            {
                return new BadRequestObjectResult(new { message = "Стадия лида не найдена" });
            }

            var lead = new Lead
            {
                Company_id = companyId,
                Name = request.Name.Trim(),
                Contact = request.Contact,
                LeadSource_id = request.LeadSourceId,
                LeadStage_id = request.LeadStageId,
                ManagerUser_id = managerUserId,
                Comment = request.Comment,
                NextTask_title = string.IsNullOrWhiteSpace(request.NextTaskTitle) ? "Первичный контакт с клиентом" : request.NextTaskTitle.Trim(),
                NextTask_due_at = request.NextTaskDueAtUtc?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(1)
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();
            return new OkObjectResult(new { message = "Лид создан", id = lead.ID_Lead });
        }

        public async Task<IActionResult> UpdateAsync(int leadId, CreateLeadRequest request, int companyId, int managerUserId)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.ID_Lead == leadId && l.Company_id == companyId);
            if (lead == null)
                return new NotFoundObjectResult(new { message = "Лид не найден" });

            var source = await _context.LeadSources.FindAsync(request.LeadSourceId);
            if (source == null)
                return new BadRequestObjectResult(new { message = "Источник лида не найден" });

            var stage = await _context.LeadStages.FindAsync(request.LeadStageId);
            if (stage == null)
                return new BadRequestObjectResult(new { message = "Стадия лида не найдена" });

            lead.Name = request.Name.Trim();
            lead.Contact = string.IsNullOrWhiteSpace(request.Contact) ? null : request.Contact.Trim();
            lead.LeadSource_id = request.LeadSourceId;
            lead.LeadStage_id = request.LeadStageId;
            lead.ManagerUser_id = managerUserId;
            lead.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
            lead.NextTask_title = string.IsNullOrWhiteSpace(request.NextTaskTitle) ? lead.NextTask_title : request.NextTaskTitle.Trim();
            lead.NextTask_due_at = request.NextTaskDueAtUtc?.ToUniversalTime() ?? lead.NextTask_due_at;
            lead.Updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return new OkObjectResult(new { message = "Лид обновлён", id = lead.ID_Lead });
        }

        public async Task<IActionResult> DeleteAsync(int leadId, int companyId)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.ID_Lead == leadId && l.Company_id == companyId);
            if (lead == null)
                return new NotFoundObjectResult(new { message = "Лид не найден" });

            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();
            return new OkObjectResult(new { deleted = true });
        }

        public async Task<IActionResult> UpdateStageAsync(int leadId, int stageId)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.ID_Lead == leadId);
            if (lead == null)
            {
                return new NotFoundObjectResult(new { message = "Лид не найден" });
            }

            var stage = await _context.LeadStages.FindAsync(stageId);
            if (stage == null)
            {
                return new BadRequestObjectResult(new { message = "Стадия лида не найдена" });
            }

            lead.LeadStage_id = stageId;
            lead.Updated_at = System.DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new OkObjectResult(new { message = "Стадия лида обновлена" });
        }

        public async Task<IActionResult> MarkLostAsync(int leadId, string reason)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.ID_Lead == leadId);
            if (lead == null)
                return new NotFoundObjectResult(new { message = "Лид не найден" });

            lead.Lost_at = DateTime.UtcNow;
            lead.Won_at = null;
            lead.Lost_reason = string.IsNullOrWhiteSpace(reason) ? "Без указания причины" : reason.Trim();
            lead.Updated_at = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new OkObjectResult(new { message = "Лид отмечен как потерян" });
        }

        public async Task<IActionResult> MarkWonAsync(int leadId)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.ID_Lead == leadId);
            if (lead == null)
                return new NotFoundObjectResult(new { message = "Лид не найден" });

            lead.Won_at = DateTime.UtcNow;
            lead.Lost_at = null;
            lead.Lost_reason = null;
            lead.Updated_at = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new OkObjectResult(new { message = "Лид отмечен как успешный" });
        }

        public async Task<IActionResult> GetAnalyticsAsync(int companyId, DateTime? fromUtc = null, DateTime? toUtc = null)
        {
            var query = _context.Leads
                .AsNoTracking()
                .Include(l => l.Stage)
                .Where(l => l.Company_id == companyId)
                .AsQueryable();

            if (fromUtc.HasValue)
            {
                var from = fromUtc.Value.ToUniversalTime();
                query = query.Where(l => l.Created_at >= from);
            }

            if (toUtc.HasValue)
            {
                var to = toUtc.Value.ToUniversalTime();
                query = query.Where(l => l.Created_at <= to);
            }

            var leads = await query.ToListAsync();
            var total = leads.Count;
            var won = leads.Count(l => l.Won_at.HasValue);
            var lost = leads.Count(l => l.Lost_at.HasValue);
            var conversion = total == 0 ? 0 : Math.Round((double)won / total * 100, 2);

            var funnel = leads
                .GroupBy(l => l.Stage.Name)
                .Select(g => new { stage = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToList();

            var lostReasons = leads
                .Where(l => !string.IsNullOrWhiteSpace(l.Lost_reason))
                .GroupBy(l => l.Lost_reason!)
                .Select(g => new { reason = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToList();

            var upcomingTasks = leads
                .Where(l => l.NextTask_due_at.HasValue && l.NextTask_due_at.Value >= DateTime.UtcNow)
                .OrderBy(l => l.NextTask_due_at)
                .Take(20)
                .Select(l => new
                {
                    leadId = l.ID_Lead,
                    leadName = l.Name,
                    taskTitle = l.NextTask_title,
                    dueAt = l.NextTask_due_at
                })
                .ToList();

            return new OkObjectResult(new
            {
                total,
                won,
                lost,
                conversionPercent = conversion,
                funnel,
                topLostReasons = lostReasons,
                upcomingTasks
            });
        }
    }
}

