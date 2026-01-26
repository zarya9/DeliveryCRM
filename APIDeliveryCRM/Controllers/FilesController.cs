using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APIDeliveryCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FilesController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file, [FromQuery] int userId)
        {
            return await _fileService.UploadAvatarAsync(file, userId);
        }

        [HttpPut("avatar")]
        public async Task<IActionResult> UpdateAvatar(IFormFile file, [FromQuery] int userId)
        {
            return await _fileService.UpdateAvatarAsync(file, userId);
        }

        [HttpGet("avatar/{userId:int}")]
        public async Task<IActionResult> GetAvatar(int userId)
        {
            return await _fileService.GetAvatarAsync(userId);
        }

        [HttpPost("report")]
        public async Task<IActionResult> UploadReport(IFormFile file, [FromQuery] int userId, [FromQuery] string reportType)
        {
            return await _fileService.UploadReportAsync(file, userId, reportType);
        }

        [HttpGet("report/{reportId:int}")]
        public async Task<IActionResult> GetReport(int reportId)
        {
            return await _fileService.GetReportAsync(reportId);
        }
    }
}

