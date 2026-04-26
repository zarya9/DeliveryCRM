using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using APIDeliveryCRM.Request;

namespace APIDeliveryCRM.Interfaces
{
    public interface ILeadService
    {
        Task<IActionResult> GetByCompanyAsync(int companyId);
        Task<IActionResult> GetMetaAsync();
        Task<IActionResult> CreateAsync(CreateLeadRequest request, int companyId, int managerUserId);
        Task<IActionResult> UpdateStageAsync(int leadId, int stageId);
        Task<IActionResult> MarkLostAsync(int leadId, string reason);
        Task<IActionResult> MarkWonAsync(int leadId);
        Task<IActionResult> GetAnalyticsAsync(int companyId, DateTime? fromUtc = null, DateTime? toUtc = null);
    }
}

