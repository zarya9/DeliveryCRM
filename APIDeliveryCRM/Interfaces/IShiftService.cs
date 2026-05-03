using System.Collections.Generic;
using System.Threading.Tasks;
using APIDeliveryCRM.Model;

namespace APIDeliveryCRM.Interfaces
{
    public interface IShiftService
    {
        Task<CourierShift> StartShiftAsync(int courierProfileId);
        Task<bool> EndShiftAsync(int shiftId);
        Task<CourierShift?> GetByIdAsync(int shiftId);
        Task<CourierShift?> GetActiveShiftAsync(int courierProfileId);
        Task<IReadOnlyList<CourierShift>> GetHistoryAsync(int courierProfileId);
        Task<IReadOnlyList<ShiftAssignment>> GetAssignmentsAsync(int shiftId);
    }
}


