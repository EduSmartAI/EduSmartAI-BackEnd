using Microsoft.AspNetCore.Http;

namespace UtilityService.Application.Interfaces;

public interface ICloudinaryService
{
    Task<string> UploadImageAsync(IFormFile file);

    Task<string> UploadImageAsync(string fileName, Stream stream, string contentType);

    Task<bool> DeleteImage(string url);

    Task<string> UploadVideoAsync(string filePath, string publicId, CancellationToken ct);
    Task<string> UploadZipAsync(IFormFile file, CancellationToken ct = default);
}