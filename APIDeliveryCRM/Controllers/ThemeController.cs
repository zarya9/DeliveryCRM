using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Request;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ThemeController : Controller
    {
        private readonly IThemeService _themeService;

        public ThemeController(IThemeService themeService)
        {
            _themeService = themeService;
        }

        [HttpPost("{userId:int}")]
        public async Task<IActionResult> SetTheme(int userId, [FromBody] SetThemeRequest request)
        {
            return await _themeService.SetThemeAsync(userId, request.ThemeCode);
        }

        [HttpGet("{userId:int}")]
        public async Task<IActionResult> GetTheme(int userId)
        {
            return await _themeService.GetThemeAsync(userId);
        }
    }
}


