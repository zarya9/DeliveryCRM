using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Interfaces
{
    public interface IScheduledReportService
    {
        Task<IActionResult> GetByCompanyAsync(int companyId);
        Task<IActionResult> UpsertAsync(int companyId, UpsertScheduledReportJobRequest request);
        Task<IActionResult> RunNowAsync(int jobId, int companyId);
        Task ExecuteDueJobsAsync(CancellationToken cancellationToken = default);
    }
}
