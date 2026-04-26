using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Interfaces
{
    public interface IFileService
    {
        Task<IActionResult> UploadAvatarAsync(IFormFile file, int userId);
        Task<IActionResult> UpdateAvatarAsync(IFormFile file, int userId);
        Task<IActionResult> GetAvatarAsync(int userId);
        Task<IActionResult> UploadReportAsync(IFormFile file, int userId, string reportType);
        Task<IActionResult> GetReportAsync(int reportId);
        Task<IActionResult> UploadChatAttachmentAsync(IFormFile file, int userId);
    }
}

