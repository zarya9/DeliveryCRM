using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services;

public class ShiftPlannerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShiftPlannerWorker> _logger;

    public ShiftPlannerWorker(IServiceScopeFactory scopeFactory, ILogger<ShiftPlannerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ContextDB>();
                var planner = scope.ServiceProvider.GetRequiredService<IShiftPlannerService>();

                var companyIds = await db.CourierShifts
                    .AsNoTracking()
                    .Where(s => s.TimeEnd == null)
                    .Select(s => s.Company_id)
                    .Distinct()
                    .ToListAsync(stoppingToken);

                foreach (var companyId in companyIds)
                {
                    try
                    {
                        await planner.RebuildCompanyPlanAsync(companyId, "worker.periodic", stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Shift planner rebuild failed for company {CompanyId}", companyId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ShiftPlannerWorker loop failed.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
