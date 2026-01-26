using System.IO;
using System.Threading.Tasks;

namespace APIDeliveryCRM.Interfaces
{
    public interface IAzureBlobService
    {
        Task<string> UploadFileAsync(string blobName, Stream fileStream, string contentType);
        Task<Stream> DownloadFileAsync(string blobName);
        Task<bool> DeleteFileAsync(string blobName);
        Task<bool> FileExistsAsync(string blobName);
        string GetBlobUrl(string blobName);
    }
}

