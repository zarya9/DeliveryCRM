using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace APIDeliveryCRM.Services
{
    public class FileService : IFileService
    {
        private string? ResolveLocalWebFilePath(string relativePath)
        {
            var normalized = (relativePath ?? string.Empty).Trim().TrimStart('~', '/', '\\');
            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            normalized = normalized.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            var primary = Path.Combine(_environment.WebRootPath ?? string.Empty, normalized);
            if (!string.IsNullOrWhiteSpace(primary) && System.IO.File.Exists(primary))
                return primary;

            var fallback = Path.Combine(_environment.ContentRootPath ?? string.Empty, "wwwroot", normalized);
            if (!string.IsNullOrWhiteSpace(fallback) && System.IO.File.Exists(fallback))
                return fallback;

            return null;
        }

        private static ContentResult SvgPlaceholderAvatar()
        {
            const string svg =
                "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"128\" height=\"128\" viewBox=\"0 0 128 128\">" +
                "<rect fill=\"#e8eaf0\" width=\"128\" height=\"128\"/>" +
                "<circle cx=\"64\" cy=\"46\" r=\"20\" fill=\"#b0b8c9\"/>" +
                "<path fill=\"#b0b8c9\" d=\"M24 122c4-28 22-42 40-42s36 14 40 42\"/></svg>";
            return new ContentResult { Content = svg, ContentType = "image/svg+xml", StatusCode = 200 };
        }

        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileService> _logger;
        private readonly ContextDB _context;
        private readonly IAzureBlobService _azureBlobService;
        private readonly bool _useAzureStorage;

        public FileService(
            IWebHostEnvironment environment, 
            ILogger<FileService> logger, 
            ContextDB context,
            IAzureBlobService azureBlobService,
            IConfiguration configuration)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
            _azureBlobService = azureBlobService;
            
            // Проверяем, настроен ли Azure Storage
            var connectionString = configuration["AzureStorage:ConnectionString"];
            _useAzureStorage = !string.IsNullOrEmpty(connectionString);
        }

        public async Task<IActionResult> UploadAvatarAsync(IFormFile file, int userId)
        {
            if (file == null || file.Length == 0)
            {
                return new BadRequestObjectResult(new { message = "Файл не выбран" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new NotFoundObjectResult(new { message = "Пользователь не найден" });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return new BadRequestObjectResult(new { message = "Недопустимый тип файла. Разрешены: jpg, jpeg, png, gif, webp" });
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return new BadRequestObjectResult(new { message = "Размер файла превышает 5MB" });
            }

            try
            {
                var fileName = $"user_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                string filePathOrUrl;

                if (_useAzureStorage)
                {
                    var blobName = $"avatars/{fileName}";
                    using var stream = file.OpenReadStream();
                    var contentType = GetContentTypeByExtension(fileExtension);
                    filePathOrUrl = await _azureBlobService.UploadFileAsync(blobName, stream, contentType);
                    _logger.LogInformation($"Аватар загружен в Azure Blob Storage: {filePathOrUrl}");
                }
                else
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "avatars");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var filePath = Path.Combine(uploadsFolder, fileName);
                    filePathOrUrl = $"/avatars/{fileName}";

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }

                user.Avatar = filePathOrUrl;
                await _context.SaveChangesAsync();

                return new OkObjectResult(new { path = filePathOrUrl, message = "Аватар успешно загружен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке аватара");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UploadReportAsync(IFormFile file, int userId, string reportType)
        {
            if (file == null || file.Length == 0)
            {
                return new BadRequestObjectResult(new { message = "Файл не выбран" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new NotFoundObjectResult(new { message = "Пользователь не найден" });
            }

            var allowedExtensions = new[] { ".pdf", ".xlsx", ".xls", ".csv" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return new BadRequestObjectResult(new { message = "Недопустимый тип файла. Разрешены: pdf, xlsx, xls, csv" });
            }

            if (file.Length > 50 * 1024 * 1024)
            {
                return new BadRequestObjectResult(new { message = "Размер файла превышает 50MB" });
            }

            try
            {
                var fileName = $"report_{userId}_{reportType}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                string filePathOrUrl;

                if (_useAzureStorage)
                {
                    var blobName = $"reports/{fileName}";
                    using var stream = file.OpenReadStream();
                    var contentType = GetContentTypeByExtension(fileExtension);
                    filePathOrUrl = await _azureBlobService.UploadFileAsync(blobName, stream, contentType);
                    _logger.LogInformation($"Отчет загружен в Azure Blob Storage: {filePathOrUrl}");
                }
                else
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "reports");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var filePath = Path.Combine(uploadsFolder, fileName);
                    filePathOrUrl = $"/reports/{fileName}";

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }

                var readyStatus = await _context.ReportStatuses.FirstOrDefaultAsync(rs => rs.Name == "Готов");
                if (readyStatus == null)
                {
                    return new BadRequestObjectResult(new { message = "Статус 'Готов' не найден в базе данных" });
                }

                var report = new APIDeliveryCRM.Model.Report
                {
                    Title = $"Отчет {reportType}",
                    Description = $"Отчет типа {reportType}, загружен пользователем {user.FName} {user.Name}",
                    FilePath = filePathOrUrl,
                    ReportType = reportType,
                    User_id = userId,
                    Status_id = readyStatus.ID_ReportStatus,
                    Created_at = DateTime.UtcNow
                };

                await _context.Reports.AddAsync(report);
                await _context.SaveChangesAsync();

                return new OkObjectResult(new { path = filePathOrUrl, reportId = report.ID_Report, message = "Отчет успешно загружен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке отчета");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UploadChatAttachmentAsync(IFormFile file, int userId)
        {
            if (file == null || file.Length == 0)
                return new BadRequestObjectResult(new { message = "Файл не выбран" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new NotFoundObjectResult(new { message = "Пользователь не найден" });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt", ".zip" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                return new BadRequestObjectResult(new { message = "Недопустимый тип файла для вложения." });

            if (file.Length > 25 * 1024 * 1024)
                return new BadRequestObjectResult(new { message = "Размер файла превышает 25MB" });

            try
            {
                var safeName = Path.GetFileNameWithoutExtension(file.FileName);
                if (safeName.Length > 60)
                    safeName = safeName[..60];

                var fileName = $"chat_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{safeName}{fileExtension}";
                string filePathOrUrl;

                if (_useAzureStorage)
                {
                    var blobName = $"chat/{fileName}";
                    await using var stream = file.OpenReadStream();
                    var contentType = GetContentTypeByExtension(fileExtension);
                    filePathOrUrl = await _azureBlobService.UploadFileAsync(blobName, stream, contentType);
                }
                else
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "chat");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var filePath = Path.Combine(uploadsFolder, fileName);
                    filePathOrUrl = $"/chat/{fileName}";
                    await using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }

                return new OkObjectResult(new { path = filePathOrUrl, fileName = file.FileName, message = "Вложение загружено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке вложения чата");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdateAvatarAsync(IFormFile file, int userId)
        {
            return await UploadAvatarAsync(file, userId);
        }

        public async Task<IActionResult> GetAvatarAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new NotFoundObjectResult(new { message = "Пользователь не найден" });
            }

            var avatarPath = user.Avatar ?? "/avatars/default.png";

            if (avatarPath.StartsWith("http://") || avatarPath.StartsWith("https://"))
            {
                try
                {
                    var uri = new Uri(avatarPath);
                    var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var blobName = string.Join("/", pathParts.Skip(1));
                    
                    var stream = await _azureBlobService.DownloadFileAsync(blobName);
                    if (stream == null)
                    {
                        _logger.LogWarning("Аватар не найден в Azure Storage: {Blob}", blobName);
                        return SvgPlaceholderAvatar();
                    }

                    var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    var contentType = GetContentTypeByExtension(Path.GetExtension(blobName));
                    return new FileStreamResult(memoryStream, contentType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при загрузке аватара из Azure Storage");
                    return SvgPlaceholderAvatar();
                }
            }
            else
            {
                var filePath = ResolveLocalWebFilePath(avatarPath);

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    var defaultPath = ResolveLocalWebFilePath("/avatars/default.png");
                    if (!string.IsNullOrWhiteSpace(defaultPath))
                    {
                        var defaultBytes = await System.IO.File.ReadAllBytesAsync(defaultPath);
                        var contentType = GetContentType(defaultPath);
                        return new FileContentResult(defaultBytes, contentType);
                    }
                    // Нет файла на диске — отдаём SVG, иначе браузер получает JSON 404 и показывает «битую» картинку.
                    return SvgPlaceholderAvatar();
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var fileContentType = GetContentType(filePath);
                return new FileContentResult(fileBytes, fileContentType);
            }
        }

        public async Task<IActionResult> GetReportAsync(int reportId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
            {
                return new NotFoundObjectResult(new { message = "Отчет не найден" });
            }

            var filePath = report.FilePath;

            if (filePath.StartsWith("http://") || filePath.StartsWith("https://"))
            {
                try
                {
                    var uri = new Uri(filePath);
                    var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var blobName = string.Join("/", pathParts.Skip(1));
                    
                    var stream = await _azureBlobService.DownloadFileAsync(blobName);
                    if (stream == null)
                    {
                        return new NotFoundObjectResult(new { message = "Файл отчета не найден в Azure Storage" });
                    }

                    var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    var contentType = GetContentTypeByExtension(Path.GetExtension(blobName));
                    var fileName = Path.GetFileName(blobName);
                    return new FileStreamResult(memoryStream, contentType)
                    {
                        FileDownloadName = fileName
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при загрузке отчета из Azure Storage");
                    return new NotFoundObjectResult(new { message = "Ошибка при загрузке отчета" });
                }
            }
            else
            {
                var localFilePath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));

                if (!System.IO.File.Exists(localFilePath))
                {
                    return new NotFoundObjectResult(new { message = "Файл отчета не найден" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(localFilePath);
                var fileContentType = GetContentType(localFilePath);
                var fileName = Path.GetFileName(localFilePath);

                return new FileContentResult(fileBytes, fileContentType)
                {
                    FileDownloadName = fileName
                };
            }
        }

        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return GetContentTypeByExtension(extension);
        }

        private string GetContentTypeByExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".csv" => "text/csv",
                ".txt" => "text/plain",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }
    }
}

