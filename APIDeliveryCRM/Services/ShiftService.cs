using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Responses;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class ShiftService : IShiftService
    {
        private readonly ContextDB _context;
        private readonly INotificationService _notificationService;
        private readonly IShiftPlannerService _plannerService;

        public ShiftService(ContextDB context, INotificationService notificationService, IShiftPlannerService plannerService)
        {
            _context = context;
            _notificationService = notificationService;
            _plannerService = plannerService;
        }

        public async Task<CourierShift> StartShiftAsync(int courierProfileId)
        {
            var activeShift = await GetActiveShiftAsync(courierProfileId);
            if (activeShift != null)
            {
                var already = await _context.CourierProfiles.FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId);
                if (already != null && !already.Is_online)
                {
                    already.Is_online = true;
                    already.LastActivity_at = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                return activeShift;
            }

            var courier = await _context.CourierProfiles.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId);
            if (courier == null)
                throw new KeyNotFoundException("Профиль курьера не найден.");

            var shift = new CourierShift
            {
                Company_id = courier.Company_id,
                Courier_id = courierProfileId,
                Date = System.DateOnly.FromDateTime(System.DateTime.UtcNow),
                TimeStart = System.DateTime.UtcNow,
                ShiftStatus_id = await GetShiftStatusIdAsync("Active")
            };

            _context.CourierShifts.Add(shift);
            var onlineCourier = await _context.CourierProfiles.FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId);
            if (onlineCourier != null)
            {
                onlineCourier.Is_online = true;
                onlineCourier.LastActivity_at = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await TryNotifyLogisticiansShiftStartedAsync(courierProfileId, courier.Company_id);
            await TryRebuildCompanyPlanAsync(courier.Company_id, "shift.started");
            return shift;
        }

        public async Task<bool> EndShiftAsync(int shiftId)
        {
            var shift = await _context.CourierShifts
                .Include(s => s.CourierProfile).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(s => s.ID_Shift == shiftId);
            if (shift == null || shift.TimeEnd != null)
            {
                return false;
            }

            shift.TimeEnd = System.DateTime.UtcNow;
            shift.ShiftStatus_id = await GetShiftStatusIdAsync("Finished");
            var courier = await _context.CourierProfiles.FirstOrDefaultAsync(c => c.ID_CourierProfile == shift.Courier_id);
            if (courier != null)
            {
                courier.Is_online = false;
                courier.LastActivity_at = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            ShiftClosureSummaryDto? closure = null;
            try
            {
                closure = await _plannerService.FinalizeShiftAsync(shiftId);
            }
            catch
            {
                // Finalize must not block shift end.
            }

            await TryNotifyLogisticiansShiftEndedAsync(shift, closure);
            await TryRebuildCompanyPlanAsync(shift.Company_id, "shift.ended");
            return true;
        }

        public async Task<CourierShift?> GetByIdAsync(int shiftId)
        {
            return await _context.CourierShifts
                .Include(s => s.CourierProfile)
                .ThenInclude(c => c.User)
                .Include(s => s.ShiftStatus)
                .FirstOrDefaultAsync(s => s.ID_Shift == shiftId);
        }

        public async Task<CourierShift?> GetActiveShiftAsync(int courierProfileId)
        {
            return await _context.CourierShifts
                .Include(s => s.CourierProfile)
                .Include(s => s.ShiftStatus)
                .Where(s => s.Courier_id == courierProfileId && s.TimeEnd == null)
                .OrderByDescending(s => s.TimeStart)
                .FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<CourierShift>> GetHistoryAsync(int courierProfileId)
        {
            return await _context.CourierShifts
                .Where(s => s.Courier_id == courierProfileId)
                .OrderByDescending(s => s.TimeStart)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<ShiftAssignment>> GetAssignmentsAsync(int shiftId)
        {
            return await _context.ShiftAssignments
                .Where(a => a.Shift_id == shiftId)
                .Include(a => a.Order)
                .ToListAsync();
        }

        private async Task<int> GetShiftStatusIdAsync(string name)
        {
            var status = await _context.ShiftStatuses.FirstOrDefaultAsync(s => s.Name == name);
            if (status != null)
            {
                if (string.IsNullOrWhiteSpace(status.Description))
                {
                    status.Description = GetShiftStatusDescription(name);
                    await _context.SaveChangesAsync();
                }
                return status.ID_ShiftStatus;
            }

            status = new ShiftStatus
            {
                Name = name,
                Description = GetShiftStatusDescription(name)
            };
            _context.ShiftStatuses.Add(status);
            await _context.SaveChangesAsync();
            return status.ID_ShiftStatus;
        }

        private static string GetShiftStatusDescription(string name) => name switch
        {
            "Active" => "Смена активна.",
            "Finished" => "Смена завершена.",
            _ => "Статус смены."
        };

        private async Task NotifyLogisticiansShiftStartedAsync(int courierProfileId, int companyId)
        {
            var courier = await _context.CourierProfiles.AsNoTracking()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierProfileId);
            if (courier?.User == null)
                return;

            var roleNames = new[] { "Логист", "Логистика" };
            var logistIds = await _context.Users.AsNoTracking()
                .Where(u => u.Company_id == companyId && roleNames.Contains(u.Role.Name))
                .Select(u => u.ID_User)
                .Distinct()
                .ToListAsync();
            if (logistIds.Count == 0)
                return;

            var notificationTypeId = await ResolveShiftNotificationTypeIdAsync(
                "Начало смены",
                "Курьер начал смену.",
                "SHIFT_STARTED");
            var fio = BuildCourierShortName(courier.User.FName, courier.User.Name, courier.User.Patronumic);
            var title = "Начало смены курьера";
            var message = $"{fio} начал смену.";

            foreach (var logistUserId in logistIds)
                await _notificationService.SendAsync(logistUserId, notificationTypeId, title, message);
        }

        private async Task NotifyLogisticiansShiftEndedAsync(CourierShift shift, ShiftClosureSummaryDto? closure)
        {
            var courier = shift.CourierProfile ?? await _context.CourierProfiles.AsNoTracking()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == shift.Courier_id);
            if (courier?.User == null)
                return;

            var companyId = shift.Company_id;
            var roleNames = new[] { "Логист", "Логистика" };
            var logistIds = await _context.Users.AsNoTracking()
                .Where(u => u.Company_id == companyId && roleNames.Contains(u.Role.Name))
                .Select(u => u.ID_User)
                .Distinct()
                .ToListAsync();
            if (logistIds.Count == 0)
                return;

            var notificationTypeId = await ResolveShiftNotificationTypeIdAsync(
                "Завершение смены",
                "Курьер завершил смену. Доступен итоговый маршрут и пересчёт топлива.",
                "SHIFT_FINISHED");

            var fio = closure?.CourierName ?? BuildCourierShortName(courier.User.FName, courier.User.Name, courier.User.Patronumic);
            var title = "Завершение смены курьера";
            string message;
            if (closure != null && closure.TotalDistanceKm > 0)
            {
                message =
                    $"{fio} завершил смену. Итоговый маршрут: {closure.TotalDistanceKm:0.#} км, " +
                    $"топливо ~{closure.EstimatedFuelLiters:0.#} л (≈ {closure.EstimatedFuelCostRub:0.#} ₽). " +
                    $"Выполнено заказов: {closure.OrdersCompletedCount}. " +
                    "Откройте итог смены для просмотра маршрута и пересчёта.";
            }
            else
            {
                message = $"{fio} завершил смену. Откройте итог смены для просмотра маршрута.";
            }

            foreach (var logistUserId in logistIds)
                await _notificationService.SendAsync(logistUserId, notificationTypeId, title, message, shiftId: shift.ID_Shift);
        }

        private async Task TryNotifyLogisticiansShiftStartedAsync(int courierProfileId, int companyId)
        {
            try
            {
                await NotifyLogisticiansShiftStartedAsync(courierProfileId, companyId);
            }
            catch
            {
                // Shift lifecycle must not fail if notification insert fails (e.g. legacy NOT NULL constraints).
            }
        }

        private async Task TryNotifyLogisticiansShiftEndedAsync(CourierShift shift, ShiftClosureSummaryDto? closure)
        {
            try
            {
                await NotifyLogisticiansShiftEndedAsync(shift, closure);
            }
            catch
            {
                // Shift lifecycle must not fail if notification insert fails.
            }
        }

        private async Task<int> ResolveShiftNotificationTypeIdAsync(string displayName, string description, string? legacyCode = null)
        {
            var type = await _context.NotificationTypes
                .FirstOrDefaultAsync(t =>
                    t.Name == displayName ||
                    (!string.IsNullOrEmpty(legacyCode) && t.Name == legacyCode));

            if (type != null)
            {
                if (!string.Equals(type.Name, displayName, StringComparison.Ordinal))
                {
                    type.Name = displayName;
                    if (string.IsNullOrWhiteSpace(type.Description))
                        type.Description = description;
                    await _context.SaveChangesAsync();
                }

                return type.ID_NotificationType;
            }

            var created = new NotificationType
            {
                Name = displayName,
                Description = description
            };
            _context.NotificationTypes.Add(created);
            await _context.SaveChangesAsync();
            return created.ID_NotificationType;
        }

        private static string BuildCourierShortName(string? first, string? last, string? patronymic)
        {
            var parts = new[] { first, last, patronymic }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .ToArray();
            return parts.Length == 0 ? "Курьер" : string.Join(' ', parts);
        }

        private async Task TryRebuildCompanyPlanAsync(int companyId, string reason)
        {
            try
            {
                await _plannerService.RebuildCompanyPlanAsync(companyId, reason);
            }
            catch
            {
                // Shift lifecycle must not fail if planner rebuild fails.
            }
        }
    }
}


