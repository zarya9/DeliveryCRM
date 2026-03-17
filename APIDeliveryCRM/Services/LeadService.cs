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
                Comment = l.Comment
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
                Comment = request.Comment
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();
            return new OkObjectResult(new { message = "Лид создан", id = lead.ID_Lead });
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
    }
}

