using System;
using System.IO;
using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace APIDeliveryCRM.Services
{
    public class AzureBlobService : IAzureBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _containerClient;
        private readonly string _containerName;
        private readonly ILogger<AzureBlobService> _logger;

        public AzureBlobService(IConfiguration configuration, ILogger<AzureBlobService> logger)
        {
            _logger = logger;
            var connectionString = configuration["AzureStorage:ConnectionString"];
            _containerName = configuration["AzureStorage:ContainerName"] ?? "deliverycrm";

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Azure Storage ConnectionString не настроен. Azure Blob Storage будет недоступен.");
                return;
            }

            try
            {
                _blobServiceClient = new BlobServiceClient(connectionString);
                _containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                
                // Создаем контейнер, если его нет
                InitializeContainerAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при инициализации Azure Blob Service");
            }
        }

        private async Task InitializeContainerAsync()
        {
            try
            {
                await _containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
                _logger.LogInformation($"Контейнер '{_containerName}' готов к использованию");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при создании контейнера '{_containerName}'");
                throw;
            }
        }

        public async Task<string> UploadFileAsync(string blobName, Stream fileStream, string contentType)
        {
            if (_containerClient == null)
            {
                throw new InvalidOperationException("Azure Blob Storage не настроен. Проверьте ConnectionString в appsettings.json");
            }

            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);

                // Сбрасываем позицию потока на начало, если нужно
                if (fileStream.CanSeek && fileStream.Position > 0)
                {
                    fileStream.Position = 0;
                }

                await blobClient.UploadAsync(fileStream, overwrite: true);

                // Устанавливаем content type
                await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders
                {
                    ContentType = contentType
                });

                _logger.LogInformation($"Файл '{blobName}' успешно загружен в Azure Blob Storage");
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при загрузке файла '{blobName}' в Azure Blob Storage");
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(string blobName)
        {
            if (_containerClient == null)
            {
                throw new InvalidOperationException("Azure Blob Storage не настроен. Проверьте ConnectionString в appsettings.json");
            }

            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                {
                    _logger.LogWarning($"Файл '{blobName}' не найден в Azure Blob Storage");
                    return null;
                }

                var response = await blobClient.DownloadAsync();
                var memoryStream = new MemoryStream();
                await response.Value.Content.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                _logger.LogInformation($"Файл '{blobName}' успешно загружен из Azure Blob Storage");
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при загрузке файла '{blobName}' из Azure Blob Storage");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string blobName)
        {
            if (_containerClient == null)
            {
                return false;
            }

            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                var result = await blobClient.DeleteIfExistsAsync();
                
                if (result.Value)
                {
                    _logger.LogInformation($"Файл '{blobName}' успешно удален из Azure Blob Storage");
                }
                
                return result.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при удалении файла '{blobName}' из Azure Blob Storage");
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string blobName)
        {
            if (_containerClient == null)
            {
                return false;
            }

            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                return await blobClient.ExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при проверке существования файла '{blobName}'");
                return false;
            }
        }

        public string GetBlobUrl(string blobName)
        {
            if (_containerClient == null)
            {
                return null;
            }

            var blobClient = _containerClient.GetBlobClient(blobName);
            return blobClient.Uri.ToString();
        }
    }
}

