using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class ScheduledReportService : IScheduledReportService
    {
        private readonly ContextDB _context;
        private readonly IReportService _reportService;
        private readonly IWebHostEnvironment _env;

        public ScheduledReportService(ContextDB context, IReportService reportService, IWebHostEnvironment env)
        {
            _context = context;
            _reportService = reportService;
            _env = env;
        }

        public async Task<IActionResult> GetByCompanyAsync(int companyId)
        {
            var list = await _context.ScheduledReportJobs
                .AsNoTracking()
                .Where(j => j.Company_id == companyId)
                .OrderBy(j => j.NextRun_at)
                .ToListAsync();
            return new OkObjectResult(list);
        }

        public async Task<IActionResult> UpsertAsync(int companyId, UpsertScheduledReportJobRequest request)
        {
            var freq = NormalizeFrequency(request.Frequency);
            if (freq is null)
                return new BadRequestObjectResult(new { message = "Frequency должен быть Daily, Weekly или Monthly." });

            if (!TimeSpan.TryParse(request.TimeUtc, out var timeUtc))
                return new BadRequestObjectResult(new { message = "TimeUtc должен быть в формате HH:mm." });

            ScheduledReportJob job;
            if (request.JobId.HasValue)
            {
                job = await _context.ScheduledReportJobs.FirstOrDefaultAsync(x => x.ID_ScheduledReportJob == request.JobId.Value && x.Company_id == companyId)
                      ?? new ScheduledReportJob { Company_id = companyId };
                if (job.ID_ScheduledReportJob == 0)
                    _context.ScheduledReportJobs.Add(job);
            }
            else
            {
                job = new ScheduledReportJob { Company_id = companyId };
                _context.ScheduledReportJobs.Add(job);
            }

            job.ReportType = request.ReportType.Trim().ToUpperInvariant();
            job.Frequency = freq;
            job.TimeUtc = request.TimeUtc;
            job.DayOfWeek = request.DayOfWeek;
            job.DayOfMonth = request.DayOfMonth;
            job.Is_active = request.Is_active;
            job.NextRun_at = CalculateNextRun(DateTime.UtcNow, job.Frequency, timeUtc, job.DayOfWeek, job.DayOfMonth);

            await _context.SaveChangesAsync();
            return new OkObjectResult(new { id = job.ID_ScheduledReportJob, nextRunAt = job.NextRun_at });
        }

        public async Task<IActionResult> RunNowAsync(int jobId, int companyId)
        {
            var job = await _context.ScheduledReportJobs
                .FirstOrDefaultAsync(j => j.ID_ScheduledReportJob == jobId && j.Company_id == companyId);
            if (job == null)
                return new NotFoundObjectResult(new { message = "Задание не найдено." });

            await ExecuteSingleJobAsync(job, CancellationToken.None);
            return new OkObjectResult(new { message = "Задание выполнено." });
        }

        public async Task ExecuteDueJobsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var due = await _context.ScheduledReportJobs
                .Where(j => j.Is_active && j.NextRun_at <= now)
                .ToListAsync(cancellationToken);

            foreach (var job in due)
            {
                await ExecuteSingleJobAsync(job, cancellationToken);
            }
        }

        private async Task ExecuteSingleJobAsync(ScheduledReportJob job, CancellationToken ct)
        {
            if (!string.Equals(job.ReportType, "FINANCE", StringComparison.OrdinalIgnoreCase))
            {
                job.LastRun_at = DateTime.UtcNow;
                job.NextRun_at = CalculateNextRun(DateTime.UtcNow, job.Frequency, TimeSpan.Parse(job.TimeUtc), job.DayOfWeek, job.DayOfMonth);
                await _context.SaveChangesAsync(ct);
                return;
            }

            var to = DateTime.UtcNow;
            var from = to.AddDays(-30);

            var result = await _reportService.ExportFinanceExcelAsync(job.Company_id, from, to);
            if (result is not FileContentResult file || file.FileContents.Length == 0)
            {
                job.LastRun_at = DateTime.UtcNow;
                job.NextRun_at = CalculateNextRun(DateTime.UtcNow, job.Frequency, TimeSpan.Parse(job.TimeUtc), job.DayOfWeek, job.DayOfMonth);
                await _context.SaveChangesAsync(ct);
                return;
            }

            var reportsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), "reports", "scheduled");
            Directory.CreateDirectory(reportsDir);
            var fileName = $"finance-auto-{job.Company_id}-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
            var fullPath = Path.Combine(reportsDir, fileName);
            await File.WriteAllBytesAsync(fullPath, file.FileContents, ct);

            var statusId = await ResolveReadyStatusIdAsync(ct);
            var userId = await ResolveSystemUserIdAsync(job.Company_id, ct);

            _context.Reports.Add(new Report
            {
                Title = "Автоотчет: Финансовая панель",
                Description = "Сформирован по расписанию",
                FilePath = $"/reports/scheduled/{fileName}",
                ReportType = "FINANCE",
                Company_id = job.Company_id,
                User_id = userId,
                Status_id = statusId,
                Created_at = DateTime.UtcNow,
                PeriodStart = from,
                PeriodEnd = to
            });

            job.LastRun_at = DateTime.UtcNow;
            job.NextRun_at = CalculateNextRun(DateTime.UtcNow, job.Frequency, TimeSpan.Parse(job.TimeUtc), job.DayOfWeek, job.DayOfMonth);
            await _context.SaveChangesAsync(ct);
        }

        private async Task<int> ResolveReadyStatusIdAsync(CancellationToken ct)
        {
            var ready = await _context.ReportStatuses
                .AsNoTracking()
                .Where(s => s.Name.ToLower().Contains("готов") || s.Name.ToLower().Contains("ready"))
                .Select(s => s.ID_ReportStatus)
                .FirstOrDefaultAsync(ct);
            if (ready != 0) return ready;
            var first = await _context.ReportStatuses.AsNoTracking().Select(s => s.ID_ReportStatus).FirstOrDefaultAsync(ct);
            if (first != 0) return first;

            var st = new ReportStatus { Name = "Готов", Description = "Сформирован" };
            _context.ReportStatuses.Add(st);
            await _context.SaveChangesAsync(ct);
            return st.ID_ReportStatus;
        }

        private async Task<int> ResolveSystemUserIdAsync(int companyId, CancellationToken ct)
        {
            var id = await _context.Users
                .AsNoTracking()
                .Where(u => u.Company_id == companyId && (u.Role.Name == "Менеджер" || u.Role.Name == "Админ"))
                .Select(u => u.ID_User)
                .FirstOrDefaultAsync(ct);
            if (id != 0) return id;

            return await _context.Users.AsNoTracking().Where(u => u.Company_id == companyId).Select(u => u.ID_User).FirstOrDefaultAsync(ct);
        }

        private static string? NormalizeFrequency(string? frequency)
        {
            var f = frequency?.Trim().ToLowerInvariant();
            return f switch
            {
                "daily" => "Daily",
                "weekly" => "Weekly",
                "monthly" => "Monthly",
                _ => null
            };
        }

        private static DateTime CalculateNextRun(DateTime nowUtc, string frequency, TimeSpan timeUtc, int? dayOfWeek, int? dayOfMonth)
        {
            var candidate = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, timeUtc.Hours, timeUtc.Minutes, 0, DateTimeKind.Utc);

            if (string.Equals(frequency, "Daily", StringComparison.OrdinalIgnoreCase))
            {
                if (candidate <= nowUtc) candidate = candidate.AddDays(1);
                return candidate;
            }

            if (string.Equals(frequency, "Weekly", StringComparison.OrdinalIgnoreCase))
            {
                var dow = dayOfWeek ?? 1;
                var delta = ((dow - (int)candidate.DayOfWeek) + 7) % 7;
                candidate = candidate.AddDays(delta);
                if (candidate <= nowUtc) candidate = candidate.AddDays(7);
                return candidate;
            }

            var dom = Math.Clamp(dayOfMonth ?? 1, 1, 28);
            var monthCandidate = new DateTime(nowUtc.Year, nowUtc.Month, dom, timeUtc.Hours, timeUtc.Minutes, 0, DateTimeKind.Utc);
            if (monthCandidate <= nowUtc) monthCandidate = monthCandidate.AddMonths(1);
            return monthCandidate;
        }
    }
}
