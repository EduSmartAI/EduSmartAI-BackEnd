using BaseService.Application.Interfaces.Repositories;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using UtilityService.Application.Interfaces;
using UtilityService.Domain.Models;

namespace UtilityService.Infrastructure.Implements;

public class CloudinaryService : ICloudinaryService
{
    private readonly ICommandRepository<CloudinaryConfig> _cloudinaryConfigRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CloudinaryService(ICommandRepository<CloudinaryConfig> cloudinaryConfigRepository, IUnitOfWork unitOfWork)
    {
        _cloudinaryConfigRepository = cloudinaryConfigRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Upload images to Cloudinary
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public async Task<string> UploadImageAsync(IFormFile file)
    {
        try
        {
            var cloudinaryKey = await _cloudinaryConfigRepository.FirstOrDefaultAsync(x => x.IsActive);
            if (cloudinaryKey == null)
            {
                throw new Exception("Cloudinary configuration not found");
            }

            var account = new Account(
                cloudinaryKey.CloudApiName,
                cloudinaryKey.CloudApiKey,
                cloudinaryKey.CloudApiSecret
            );
            var cloudinary = new Cloudinary(account);

            ImageUploadResult uploadResult;
            await using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, stream)
                };

                uploadResult = await cloudinary.UploadAsync(uploadParams);
            }

            if (uploadResult.Error != null)
            {
                // If rate limit exceeded, switch to a new key and retry
                if (uploadResult.Error.Message.Contains("Rate Limit Exceeded", StringComparison.OrdinalIgnoreCase))
                {
                    _cloudinaryConfigRepository.Update(cloudinaryKey, "Admin");
                    await _unitOfWork.SaveChangesAsync(CancellationToken.None);

                    // Set current key to inactive
                    var nextKey = await _cloudinaryConfigRepository.FirstOrDefaultAsync(x => x.IsActive);
                    if (nextKey == null)
                    {
                        throw new Exception("No Cloudinary API keys available.");
                    }

                    // Gọi lại upload với key mới
                    return await RetryWithNewKey(file, nextKey);
                }

                throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl?.ToString() ?? throw new Exception("Upload failed");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    public async Task<string> UploadImageAsync(string fileName, Stream stream, string contentType)
    {
        try
        {
            var cloudinaryKey = await _cloudinaryConfigRepository.FirstOrDefaultAsync(x => x.IsActive);
            if (cloudinaryKey == null)
                throw new Exception("Không tìm thấy cấu hình Cloudinary");

            var account = new Account(
                cloudinaryKey.CloudApiName,
                cloudinaryKey.CloudApiKey,
                cloudinaryKey.CloudApiSecret
            );
            var cloudinary = new Cloudinary(account);

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream)
            };

            var uploadResult = await cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                // Nếu key bị rate limit
                if (uploadResult.Error.Message.Contains("Rate Limit Exceeded", StringComparison.OrdinalIgnoreCase) || 
                    uploadResult.Error.Message.Contains("Too many requests", StringComparison.OrdinalIgnoreCase) ||
                    uploadResult.Error.Message.Contains("cloud_name is disabled", StringComparison.OrdinalIgnoreCase))
                {
                    _cloudinaryConfigRepository.Update(cloudinaryKey, uploadResult.Error.Message, true);
                    await _unitOfWork.SaveChangesAsync(CancellationToken.None);

                    var nextKey = await _cloudinaryConfigRepository.FirstOrDefaultAsync(x => x.IsActive);
                    if (nextKey == null)
                        throw new Exception("Không còn Cloudinary API key khả dụng.");

                    return await RetryWithNewKey(fileName, stream, nextKey);
                }

                throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl?.ToString() ?? throw new Exception("Upload thất bại");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

private async Task<string> RetryWithNewKey(string fileName, Stream stream, CloudinaryConfig newKey)
{
    var account = new Account(newKey.CloudApiName, newKey.CloudApiKey, newKey.CloudApiSecret);
    var cloudinary = new Cloudinary(account);

    stream.Position = 0; // reset stream
    var uploadParams = new ImageUploadParams
    {
        File = new FileDescription(fileName, stream)
    };

    var uploadResult = await cloudinary.UploadAsync(uploadParams);
    if (uploadResult.Error != null)
        throw new Exception($"Retry upload failed: {uploadResult.Error.Message}");

    return uploadResult.SecureUrl?.ToString() ?? throw new Exception("Retry upload failed");
}

    /// <summary>
    /// Deletes an image from Cloudinary using its URL.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public async Task<bool> DeleteImage(string url)
    {
        // var deletionParams = new DeletionParams(ExtractPublicId(url))
        // {
        //     ResourceType = ResourceType.Image
        // };
        //
        // var result = await _cloudinary.DestroyAsync(deletionParams);
        // return result.Result == "ok"; 
        return true;
    }

    /// <summary>
    /// Extracts the public ID from the Cloudinary image URL.
    /// </summary>
    /// <param name="imageUrl"></param>
    /// <returns></returns>
    private string? ExtractPublicId(string imageUrl)
    {
        // Find Public Id
        var match = Regex.Match(imageUrl, @"/upload/v\d+/(.*)\..+$");
        return match.Success ? match.Groups[1].Value : null;
    }

    private async Task<string> RetryWithNewKey(IFormFile file, CloudinaryConfig config)
    {
        var account = new Account(
            config.CloudApiName,
            config.CloudApiKey,
            config.CloudApiSecret
        );
        var cloudinary = new Cloudinary(account);

        ImageUploadResult uploadResult;
        await using (var stream = file.OpenReadStream())
        {
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, stream)
            };

            uploadResult = await cloudinary.UploadAsync(uploadParams);
        }

        if (uploadResult.Error != null)
        {
            throw new Exception($"Cloudinary retry upload failed: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl?.ToString() ?? throw new Exception("Retry upload failed");
    }

    public async Task<string> UploadVideoAsync(string filePath, string publicId, CancellationToken ct)
    {
        try
        {
            var cloudinaryKey = await _cloudinaryConfigRepository.FirstOrDefaultAsync(x => x.IsActive);
            if (cloudinaryKey == null)
            {
                throw new Exception("Cloudinary configuration not found");
            }

            var account = new Account(
                cloudinaryKey.CloudApiName,
                cloudinaryKey.CloudApiKey,
                cloudinaryKey.CloudApiSecret
            );
            var cloudinary = new Cloudinary(account);

            await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(Path.GetFileName(filePath), fs),
                PublicId = publicId,
                Overwrite = true,
                EagerTransforms = new List<Transformation> {
            new Transformation().StreamingProfile("auto:maxres_2160p")
            },
                EagerAsync = true,
                NotificationUrl = "https://794d5fd13b7f.ngrok-free.app/api/cloudinary/webhook"
            };

            var result = await cloudinary.UploadLargeAsync<VideoUploadResult>(
                uploadParams,
                bufferSize: 8 * 1024 * 1024,
                cancellationToken: ct
            );

            if (result.Error != null)
                throw new Exception(result.Error.Message);

            var hlsUrl = cloudinary.Api.UrlVideoUp
                .Transform(new Transformation().StreamingProfile("auto:maxres_2160p"))
                .Format("m3u8")
                .Version(result.Version?.ToString())
                .BuildUrl(result.PublicId);
            Console.WriteLine(hlsUrl);
            return hlsUrl;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
    }

    public async Task<string> UploadZipAsync(IFormFile file, CancellationToken ct = default)
    {
        try
        {
            // Get Key
            var cloudinaryKey = await _cloudinaryConfigRepository.FirstOrDefaultAsync(x => x.IsActive);
            if (cloudinaryKey == null)
            {
                throw new Exception("Cloudinary configuration not found");
            }

            var account = new Account(
                cloudinaryKey.CloudApiName,
                cloudinaryKey.CloudApiKey,
                cloudinaryKey.CloudApiSecret
            );
            var cloudinary = new Cloudinary(account);


            var publicId = $"{Guid.NewGuid():N}.zip";

            await using var stream = file.OpenReadStream();

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "zips",
                PublicId = publicId,
                UseFilename = false,
                UniqueFilename = false,
                Overwrite = false,
            };

            RawUploadResult result = await cloudinary.UploadLargeAsync<RawUploadResult>(
                uploadParams,
                bufferSize: 6 * 1024 * 1024,
                cancellationToken: ct
            );

            if (result.Error != null)
                throw new Exception($"Cloudinary upload failed: {result.Error.Message}");

            return result.SecureUrl?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
    }
}