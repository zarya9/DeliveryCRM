using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Interfaces
{
    public interface ICommunicationTemplateService
    {
        Task<IActionResult> GetByCompanyAsync(int companyId);
        Task<IActionResult> UpsertAsync(int companyId, UpsertCommunicationTemplateRequest request);
        Task<CommunicationTemplate?> ResolveForOrderStatusAsync(int companyId, int statusId);
        string Render(string template, Order order, string? statusName);
    }
}
