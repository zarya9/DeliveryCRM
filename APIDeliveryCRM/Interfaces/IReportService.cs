using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Interfaces;

public interface IReportService
{
    Task<IActionResult> GetFinanceDashboardAsync(int companyId, DateTime? fromUtc, DateTime? toUtc);
    Task<IActionResult> ExportFinanceExcelAsync(int companyId, DateTime? fromUtc, DateTime? toUtc);
}
