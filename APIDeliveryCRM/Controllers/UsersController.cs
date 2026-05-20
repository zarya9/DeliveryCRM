using System.Security.Claims;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IPasswordResetService _passwordResetService;
        private readonly ContextDB _context;

        public UsersController(
            IUserLoginService userService,
            IUserPresenceService presenceService,
            IPasswordResetService passwordResetService,
            ContextDB context)
        {
            _userService = userService;
            _presenceService = presenceService;
            _passwordResetService = passwordResetService;
            _context = context;
        }

        [HttpGet("online")]
        [Authorize]
        public async Task<IActionResult> GetOnlineUsers([FromQuery] int? companyId = null)
        {
            var resolvedCompanyId = ResolveCompanyId(companyId, out var forbidden);
            if (forbidden) return Forbid();
            if (!resolvedCompanyId.HasValue) return Unauthorized();

            var onlineIds = _presenceService.GetOnlineUserIds().ToHashSet();
            if (onlineIds.Count == 0)
                return Ok(new List<int>());

            var companyOnlineIds = await _context.Users
                .AsNoTracking()
                .Where(u => u.Company_id == resolvedCompanyId.Value && onlineIds.Contains(u.ID_User))
                .Select(u => u.ID_User)
                .ToListAsync();

            return Ok(companyOnlineIds);
        }

        [HttpGet]
        [Route("getById")]
        [Authorize]
        public async Task<IActionResult> GetUserByIdAsync(int id)
        {
            return await _userService.GetUserByIdAsync(id);
        }

        [HttpGet]
        [Route("getAllUsers")]
        [Authorize]
        public async Task<IActionResult> GetAllUsersAsync()
        {
            return await _userService.GetAllUsersAsync();
        }

        [HttpPost]
        [Route("RegisterClient")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterClientAsync(RegisterClientRequest dto)
        {
            return await _userService.RegisterClientAsync(dto);
        }

        [HttpPost]
        [Route("RegisterCompanyOwner")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterCompanyOwnerAsync(RegisterCompanyOwnerRequest dto)
        {
            return await _userService.RegisterCompanyOwnerAsync(dto);
        }

        [HttpPost]
        [Route("RegisterManager")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterManagerAsync(RegisterManagerRequest dto)
        {
            return await _userService.RegisterManagerAsync(dto);
        }

        [HttpPost]
        [Route("RegisterLogistician")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterLogisticianAsync(RegisterLogisticianRequest dto)
        {
            return await _userService.RegisterLogisticianAsync(dto);
        }

        [HttpPost]
        [Route("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginAsync(LoginRequest dto)
        {
            return await _userService.LoginAsync(dto);
        }

        [HttpPost("password-reset/request")]
        [AllowAnonymous]
        public Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetRequest dto)
        {
            return _passwordResetService.RequestResetCodeAsync(dto, HttpContext.RequestAborted);
        }

        [HttpPost("password-reset/complete")]
        [AllowAnonymous]
        public Task<IActionResult> CompletePasswordReset([FromBody] CompletePasswordResetRequest dto)
        {
            return _passwordResetService.CompleteResetAsync(dto, HttpContext.RequestAborted);
        }

        [HttpPost]
        [Route("RegisterCourier")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterCourierAsync(RegisterCourierRequest dto)
        {
            return await _userService.RegisterCourierAsync(dto);
        }

        [HttpGet]
        [Route("GetAllManagers")]
        [Authorize]
        public async Task<IActionResult> GetAllManagersAsync()
        {
            return await _userService.GetAllManagersAsync();
        }

        [HttpGet]
        [Route("GetAllCourier")]
        [Authorize]
        public async Task<IActionResult> GetAllCourierAsync()
        {
            return await _userService.GetAllCourierAsync();
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateUserRequest request)
        {
            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idStr, out var id))
                return Unauthorized();
            return await _userService.UpdateUserAsync(id, request);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idStr, out var currentId) || currentId != id)
                return Forbid();
            return await _userService.UpdateUserAsync(id, request);
        }

        private int? ResolveCompanyId(int? requestedCompanyId, out bool forbidden)
        {
            forbidden = false;
            var raw = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(raw, out var claimCompanyId))
                return null;

            if (requestedCompanyId.HasValue && requestedCompanyId.Value != claimCompanyId)
            {
                forbidden = true;
                return null;
            }

            return claimCompanyId;
        }
    }
}


