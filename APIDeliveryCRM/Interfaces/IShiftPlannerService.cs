using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APIDeliveryCRM.Request;
using APIDeliveryCRM.Responses;

namespace APIDeliveryCRM.Interfaces;

public interface IShiftPlannerService
{
    Task<CompanyPlannerResultDto> RebuildCompanyPlanAsync(int companyId, string reason, CancellationToken cancellationToken = default);

    Task<CompanyPlannerResultDto> GetCompanyPlanAsync(int companyId, CancellationToken cancellationToken = default);

    Task<ShiftPlanSummaryDto?> GetActivePlanForCourierAsync(int courierProfileId, CancellationToken cancellationToken = default);

    Task<ShiftPlanSummaryDto?> GetCourierPlanAsync(int courierProfileId, CancellationToken cancellationToken = default);

    Task<(ShiftPlanSummaryDto? Plan, string? Error)> ApplyCourierRouteAsync(
        int companyId,
        int courierProfileId,
        IReadOnlyList<ApplyCourierRouteStopRequest> stops,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<ShiftClosureSummaryDto?> FinalizeShiftAsync(int shiftId, CancellationToken cancellationToken = default);

    Task<ShiftClosureSummaryDto?> GetShiftClosureSummaryAsync(int shiftId, CancellationToken cancellationToken = default);

    Task RecalculateActivePlanDistanceAsync(int courierProfileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CourierRouteMapWaypointDto>> GetCourierRouteWaypointsAsync(
        int courierProfileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CourierRouteMapWaypointDto>> GetShiftRouteWaypointsAsync(
        int shiftId,
        CancellationToken cancellationToken = default);

    Task<bool> IsCourierOwnedByUserAsync(int courierProfileId, int userId, CancellationToken cancellationToken = default);
}
