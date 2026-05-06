using System.Threading;
using System.Threading.Tasks;
using APIDeliveryCRM.Responses;

namespace APIDeliveryCRM.Interfaces;

public interface IShiftPlannerService
{
    Task<CompanyPlannerResultDto> RebuildCompanyPlanAsync(int companyId, string reason, CancellationToken cancellationToken = default);

    Task<CompanyPlannerResultDto> GetCompanyPlanAsync(int companyId, CancellationToken cancellationToken = default);

    Task<ShiftPlanSummaryDto?> GetActivePlanForCourierAsync(int courierProfileId, CancellationToken cancellationToken = default);
}
