using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Interfaces
{
    public interface IThemeService
    {
        Task<IActionResult> SetThemeAsync(int userId, string themeCode);
        Task<IActionResult> GetThemeAsync(int userId);
    }
}


