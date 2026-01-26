using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Services
{
    public class ThemeService : IThemeService
    {
        private readonly ContextDB _context;

        public ThemeService(ContextDB context)
        {
            _context = context;
        }

        public async Task<IActionResult> SetThemeAsync(int userId, string themeCode)
        {
            if (string.IsNullOrEmpty(themeCode))
            {
                return new BadRequestObjectResult(new { message = "Код темы не может быть пустым" });
            }

            var validThemes = new[] { "light", "dark" };
            if (!validThemes.Contains(themeCode.ToLower()))
            {
                return new BadRequestObjectResult(new { message = $"Недопустимая тема. Разрешены: {string.Join(", ", validThemes)}" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new NotFoundObjectResult(new { message = "Пользователь не найден" });
            }

            user.Theme = themeCode.ToLower();
            await _context.SaveChangesAsync();

            return new OkObjectResult(new { message = "Тема успешно изменена", theme = user.Theme });
        }

        public async Task<IActionResult> GetThemeAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new NotFoundObjectResult(new { message = "Пользователь не найден" });
            }

            return new OkObjectResult(new { theme = user.Theme ?? "light" });
        }
    }
}


