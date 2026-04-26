using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly IUserLoginService _userService;
        private readonly IUserPresenceService _presenceService;
        private readonly ContextDB _context;

        public UsersController(IUserLoginService userService, IUserPresenceService presenceService, ContextDB context)
        {
            _userService = userService;
            _presenceService = presenceService;
            _context = context;
        }

        /// <summary>Список id пользователей компании, у которых есть активное SignalR-подключение (чат).</summary>
        [HttpGet("online")]
        public async Task<IActionResult> GetOnlineUsers([FromQuery] int companyId)
        {
            var onlineIds = _presenceService.GetOnlineUserIds().ToHashSet();
            if (onlineIds.Count == 0)
                return Ok(new List<int>());

            var companyOnlineIds = await _context.Users
                .AsNoTracking()
                .Where(u => u.Company_id == companyId && onlineIds.Contains(u.ID_User))
                .Select(u => u.ID_User)
                .ToListAsync();

            return Ok(companyOnlineIds);
        }

        [HttpGet]
        [Route("getById")]
        public async Task<IActionResult> GetUserByIdAsync(int id)
        {
            return await _userService.GetUserByIdAsync(id);
        }

        [HttpGet]
        [Route("getAllUsers")]
        public async Task<IActionResult> GetAllUsersAsync()
        {
            return await _userService.GetAllUsersAsync();
        }

        [HttpPost]
        [Route("RegisterClient")]
        public async Task<IActionResult> RegisterClientAsync(RegisterClientRequest dto)
        {
            return await _userService.RegisterClientAsync(dto);
        }

        [HttpPost]
        [Route("RegisterManager")]
        public async Task<IActionResult> RegisterManagerAsync(RegisterManagerRequest dto)
        {
            return await _userService.RegisterManagerAsync(dto);
        }

        [HttpPost]
        [Route("RegisterLogistician")]
        public async Task<IActionResult> RegisterLogisticianAsync(RegisterLogisticianRequest dto)
        {
            return await _userService.RegisterLogisticianAsync(dto);
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> LoginAsync(LoginRequest dto)
        {
            return await _userService.LoginAsync(dto);
        }

        [HttpPost]
        [Route("RegisterCourier")]
        public async Task<IActionResult> RegisterCourierAsync(RegisterCourierRequest dto)
        {
            return await _userService.RegisterCourierAsync(dto);
        }

        [HttpGet]
        [Route("GetAllManagers")]
        public async Task<IActionResult> GetAllManagersAsync()
        {
            return await _userService.GetAllManagersAsync();
        }

        [HttpGet]
        [Route("GetAllCourier")]
        public async Task<IActionResult> GetAllCourierAsync()
        {
            return await _userService.GetAllCourierAsync();
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            return await _userService.UpdateUserAsync(id, request);
        }
    }
}


