using System.Threading.Tasks;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Interfaces;

public interface ICompanySettingsService
{
    Task<IActionResult> GetSlaSettingsAsync(int companyId);
    Task<IActionResult> UpdateSlaSettingsAsync(int companyId, UpdateCompanySlaSettingsRequest request);
}

