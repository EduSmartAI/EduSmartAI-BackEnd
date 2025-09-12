namespace UtilityService.Application.Interfaces;

public interface IFileUploadStatusService
{
    Task CreateUploadStatusAsync(Guid requestId, string userId, string status);
    Task UpdateUploadStatusAsync(Guid requestId, string status, string? fileUrl, string? errorMessage = null);
    // Task<UploadStatusEntity?> GetUploadStatusAsync(Guid requestId, string userId);

}