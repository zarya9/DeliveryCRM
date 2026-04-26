using System.Threading.Tasks;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Interfaces
{
    public interface ISupportTicketService
    {
        Task<IActionResult> GetByCompanyAsync(int companyId, byte? status = null, byte? priority = null, bool onlyOverdue = false);
        Task<IActionResult> CreateAsync(CreateSupportTicketRequest request, int companyId, int createdByUserId);
        Task<IActionResult> AssignAsync(int ticketId, int responsibleUserId, int actorUserId);
        Task<IActionResult> UpdateStatusAsync(int ticketId, UpdateSupportTicketStatusRequest request, int actorUserId);
        Task<IActionResult> GetAnalyticsAsync(int companyId);
    }
}
