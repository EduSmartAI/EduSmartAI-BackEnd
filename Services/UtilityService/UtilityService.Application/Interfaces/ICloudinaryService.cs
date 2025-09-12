using Microsoft.AspNetCore.Http;

namespace UtilityService.Application.Interfaces;

public interface ICloudinaryService
{
    Task<string> UploadImageAsync(IFormFile file);

    Task<bool> DeleteImage(string url);
}