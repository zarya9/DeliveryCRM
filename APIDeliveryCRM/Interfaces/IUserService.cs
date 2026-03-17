using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace APIDeliveryCRM.Interfaces
{
    public interface IUserLoginService
    {   
        Task<IActionResult> GetUserByIdAsync(int id);
        Task<IActionResult> GetAllUsersAsync();
        Task<IActionResult> RegisterClientAsync(RegisterClientRequest dto);
        Task<IActionResult> RegisterManagerAsync(RegisterManagerRequest dto);
        Task<IActionResult> RegisterLogisticianAsync(RegisterLogisticianRequest dto);
        Task<IActionResult> LoginAsync(LoginRequest dto);
        Task<IActionResult> RegisterCourierAsync(RegisterCourierRequest dto);
        Task<IActionResult> UpdateUserAsync(int userId, UpdateUserRequest request);
        Task<IActionResult> GetAllManagersAsync();
        Task<IActionResult> GetAllCourierAsync();
    }
}


