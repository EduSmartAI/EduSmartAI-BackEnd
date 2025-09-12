using System.Text.RegularExpressions;
using BaseService.Application.Interfaces.Repositories;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
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
}