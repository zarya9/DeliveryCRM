using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class ShiftService : IShiftService
    {
        private readonly ContextDB _context;

        public ShiftService(ContextDB context)
        {
            _context = context;
        }

        public async Task<CourierShift> StartShiftAsync(int courierProfileId)
        {
            var activeShift = await GetActiveShiftAsync(courierProfileId);
            if (activeShift != null)
            {
                return activeShift;
            }

            var shift = new CourierShift
            {
                Courier_id = courierProfileId,
                Date = System.DateOnly.FromDateTime(System.DateTime.UtcNow),
                TimeStart = System.DateTime.UtcNow,
                ShiftStatus_id = await GetShiftStatusIdAsync("Active")
            };

            _context.CourierShifts.Add(shift);
            await _context.SaveChangesAsync();
            return shift;
        }

        public async Task<bool> EndShiftAsync(int shiftId)
        {
            var shift = await _context.CourierShifts.FirstOrDefaultAsync(s => s.ID_Shift == shiftId);
            if (shift == null || shift.TimeEnd != null)
            {
                return false;
            }

            shift.TimeEnd = System.DateTime.UtcNow;
            shift.ShiftStatus_id = await GetShiftStatusIdAsync("Finished");
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CourierShift> GetActiveShiftAsync(int courierProfileId)
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
                return status.ID_ShiftStatus;
            }

            status = new ShiftStatus
            {
                Name = name
            };
            _context.ShiftStatuses.Add(status);
            await _context.SaveChangesAsync();
            return status.ID_ShiftStatus;
        }
    }
}


