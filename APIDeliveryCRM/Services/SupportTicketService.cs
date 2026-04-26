using System;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using APIDeliveryCRM.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class SupportTicketService : ISupportTicketService
    {
        private readonly ContextDB _context;
        private readonly INotificationService _notificationService;

        public SupportTicketService(ContextDB context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> GetByCompanyAsync(int companyId, byte? status = null, byte? priority = null, bool onlyOverdue = false)
        {
            var query = _context.SupportTickets
                .Include(t => t.ResponsibleUser)
                .Where(t => t.Company_id == companyId)
                .AsQueryable();

            if (status.HasValue)
            {
                var s = (SupportTicketStatus)status.Value;
                query = query.Where(t => t.Status == s);
            }

            if (priority.HasValue)
                query = query.Where(t => t.Priority == priority.Value);

            if (onlyOverdue)
                query = query.Where(t => t.Sla_due_at.HasValue && t.Sla_due_at.Value < DateTime.UtcNow && t.Status != SupportTicketStatus.Resolved && t.Status != SupportTicketStatus.Closed);

            var items = await query
                .OrderByDescending(t => t.Created_at)
                .Take(500)
                .ToListAsync();

            return new OkObjectResult(items.Select(MapToDto).ToList());
        }

        public async Task<IActionResult> CreateAsync(CreateSupportTicketRequest request, int companyId, int createdByUserId)
        {
            if (!Enum.IsDefined(typeof(SupportTicketCategory), request.Category))
                return new BadRequestObjectResult(new { message = "Некорректная категория обращения." });

            if (!await _context.Users.AsNoTracking().AnyAsync(u => u.ID_User == createdByUserId && u.Company_id == companyId))
                return new BadRequestObjectResult(new { message = "Создатель обращения не найден в компании." });

            if (request.Order_id.HasValue)
            {
                var orderExists = await _context.Orders.AsNoTracking()
                    .AnyAsync(o => o.ID_Order == request.Order_id.Value && o.Company_id == companyId);
                if (!orderExists)
                    return new BadRequestObjectResult(new { message = "Связанный заказ не найден." });
            }

            if (request.ClientProfile_id.HasValue)
            {
                var clientExists = await _context.ClientProfiles.AsNoTracking()
                    .AnyAsync(c => c.ID_ClientProfile == request.ClientProfile_id.Value && c.Company_id == companyId);
                if (!clientExists)
                    return new BadRequestObjectResult(new { message = "Связанный клиент не найден." });
            }

            int? responsibleUserId = request.ResponsibleUser_id;
            if (responsibleUserId.HasValue)
            {
                var responsibleExists = await _context.Users.AsNoTracking()
                    .AnyAsync(u => u.ID_User == responsibleUserId.Value && u.Company_id == companyId);
                if (!responsibleExists)
                    return new BadRequestObjectResult(new { message = "Ответственный сотрудник не найден." });
            }

            var now = DateTime.UtcNow;
            var dueAt = CalculateSlaDueAt(now, request.Priority);

            var ticket = new SupportTicket
            {
                Company_id = companyId,
                Order_id = request.Order_id,
                ClientProfile_id = request.ClientProfile_id,
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                Category = (SupportTicketCategory)request.Category,
                Priority = request.Priority,
                Status = SupportTicketStatus.New,
                ResponsibleUser_id = responsibleUserId,
                CreatedByUser_id = createdByUserId,
                Created_at = now,
                Sla_due_at = dueAt
            };

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            if (responsibleUserId.HasValue)
            {
                var typeId = await ResolveNotificationTypeIdAsync();
                if (typeId > 0)
                {
                    await _notificationService.SendAsync(
                        responsibleUserId.Value,
                        typeId,
                        "Назначено новое обращение",
                        $"Обращение #{ticket.ID_SupportTicket}: {ticket.Title}",
                        ticket.Order_id);
                }
            }

            return new OkObjectResult(new
            {
                message = "Обращение создано",
                id = ticket.ID_SupportTicket,
                slaDueAt = ticket.Sla_due_at
            });
        }

        public async Task<IActionResult> AssignAsync(int ticketId, int responsibleUserId, int actorUserId)
        {
            var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.ID_SupportTicket == ticketId);
            if (ticket == null)
                return new NotFoundObjectResult(new { message = "Обращение не найдено." });

            var responsible = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.ID_User == responsibleUserId && u.Company_id == ticket.Company_id);
            if (responsible == null)
                return new BadRequestObjectResult(new { message = "Ответственный сотрудник не найден." });

            ticket.ResponsibleUser_id = responsibleUserId;
            ticket.FirstResponse_at ??= DateTime.UtcNow;
            if (ticket.Status == SupportTicketStatus.New)
                ticket.Status = SupportTicketStatus.InProgress;

            await _context.SaveChangesAsync();

            var typeId = await ResolveNotificationTypeIdAsync();
            if (typeId > 0)
            {
                await _notificationService.SendAsync(
                    responsibleUserId,
                    typeId,
                    "Вам назначено обращение",
                    $"Обращение #{ticket.ID_SupportTicket}: {ticket.Title}",
                    ticket.Order_id);
            }

            return new OkObjectResult(new { message = "Ответственный назначен", actorUserId });
        }

        public async Task<IActionResult> UpdateStatusAsync(int ticketId, UpdateSupportTicketStatusRequest request, int actorUserId)
        {
            if (!Enum.IsDefined(typeof(SupportTicketStatus), request.Status))
                return new BadRequestObjectResult(new { message = "Некорректный статус обращения." });

            var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.ID_SupportTicket == ticketId);
            if (ticket == null)
                return new NotFoundObjectResult(new { message = "Обращение не найдено." });

            var newStatus = (SupportTicketStatus)request.Status;
            ticket.Status = newStatus;
            ticket.Delay_reason = string.IsNullOrWhiteSpace(request.DelayReason) ? null : request.DelayReason.Trim();
            if (newStatus == SupportTicketStatus.InProgress)
                ticket.FirstResponse_at ??= DateTime.UtcNow;
            if (newStatus == SupportTicketStatus.Resolved || newStatus == SupportTicketStatus.Closed)
                ticket.Resolved_at ??= DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new OkObjectResult(new { message = "Статус обращения обновлен", actorUserId });
        }

        public async Task<IActionResult> GetAnalyticsAsync(int companyId)
        {
            var now = DateTime.UtcNow;
            var tickets = await _context.SupportTickets
                .AsNoTracking()
                .Where(t => t.Company_id == companyId)
                .ToListAsync();

            var overdue = tickets.Count(t =>
                t.Sla_due_at.HasValue &&
                t.Sla_due_at.Value < now &&
                t.Status != SupportTicketStatus.Resolved &&
                t.Status != SupportTicketStatus.Closed);

            var byCategory = tickets
                .GroupBy(t => t.Category.ToString())
                .Select(g => new { category = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToList();

            var byStatus = tickets
                .GroupBy(t => t.Status.ToString())
                .Select(g => new { status = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToList();

            var delayReasons = tickets
                .Where(t => !string.IsNullOrWhiteSpace(t.Delay_reason))
                .GroupBy(t => t.Delay_reason!)
                .Select(g => new { reason = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToList();

            return new OkObjectResult(new
            {
                total = tickets.Count,
                overdue,
                byCategory,
                byStatus,
                topDelayReasons = delayReasons
            });
        }

        private static DateTime CalculateSlaDueAt(DateTime createdAtUtc, byte priority)
        {
            return priority switch
            {
                2 => createdAtUtc.AddHours(4),
                1 => createdAtUtc.AddHours(12),
                _ => createdAtUtc.AddHours(24)
            };
        }

        private async Task<int> ResolveNotificationTypeIdAsync()
        {
            var ticketTypeId = await _context.NotificationTypes
                .AsNoTracking()
                .Where(t => t.Name.ToLower().Contains("ticket") || t.Name.ToLower().Contains("обращ"))
                .Select(t => t.ID_NotificationType)
                .FirstOrDefaultAsync();

            if (ticketTypeId != 0)
                return ticketTypeId;

            return await _context.NotificationTypes
                .AsNoTracking()
                .Select(t => t.ID_NotificationType)
                .FirstOrDefaultAsync();
        }

        private static SupportTicketDto MapToDto(SupportTicket t)
        {
            var isOverdue = t.Sla_due_at.HasValue &&
                            t.Sla_due_at.Value < DateTime.UtcNow &&
                            t.Status != SupportTicketStatus.Resolved &&
                            t.Status != SupportTicketStatus.Closed;

            return new SupportTicketDto
            {
                Id = t.ID_SupportTicket,
                CompanyId = t.Company_id,
                OrderId = t.Order_id,
                ClientProfileId = t.ClientProfile_id,
                Title = t.Title,
                Description = t.Description,
                Category = t.Category.ToString(),
                Priority = t.Priority,
                Status = t.Status.ToString(),
                ResponsibleUserId = t.ResponsibleUser_id,
                ResponsibleUserName = t.ResponsibleUser != null ? $"{t.ResponsibleUser.FName} {t.ResponsibleUser.Name}" : null,
                CreatedByUserId = t.CreatedByUser_id,
                CreatedAt = t.Created_at,
                FirstResponseAt = t.FirstResponse_at,
                ResolvedAt = t.Resolved_at,
                SlaDueAt = t.Sla_due_at,
                IsSlaOverdue = isOverdue,
                DelayReason = t.Delay_reason
            };
        }
    }
}
