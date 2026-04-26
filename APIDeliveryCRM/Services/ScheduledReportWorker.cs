using APIDeliveryCRM.Interfaces;

namespace APIDeliveryCRM.Services
{
    public class ScheduledReportWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ScheduledReportWorker> _logger;

        public ScheduledReportWorker(IServiceScopeFactory scopeFactory, ILogger<ScheduledReportWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var scheduler = scope.ServiceProvider.GetRequiredService<IScheduledReportService>();
                    await scheduler.ExecuteDueJobsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка выполнения ScheduledReportWorker.");
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
}
