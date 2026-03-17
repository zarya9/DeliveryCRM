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
    }
}

