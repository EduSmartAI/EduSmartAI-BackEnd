using StackExchange.Redis;
using UtilityService.Application.Interfaces;
using IDatabase = StackExchange.Redis.IDatabase;

namespace UtilityService.Infrastructure.Implements;

public class FileUploadStatusService : IFileUploadStatusService
{
    private readonly IDatabase _redisDb;

    public FileUploadStatusService(IConnectionMultiplexer redis)
    {
        _redisDb = redis.GetDatabase();
    }

    public Task CreateUploadStatusAsync(Guid requestId, string userId, string status)
    {
        // var statusEntity = new UploadStatusEntity
        // {
        //     RequestId = requestId,
        //     UserId = userId,
        //     Status = status,
        //     CreatedAt = DateTime.UtcNow,
        //     UpdatedAt = DateTime.UtcNow
        // };
        //
        // _redisDb.Set($"upload_{requestId}", statusEntity, TimeSpan.FromHours(24));
        // return Task.CompletedTask;
        return null;
    }

    public Task UpdateUploadStatusAsync(Guid requestId, string status, string? fileUrl, string? errorMessage = null)
    {
        // if (_cache.TryGetValue($"upload_{requestId}", out UploadStatusEntity? existingStatus))
        // {
        //     existingStatus.Status = status;
        //     existingStatus.FileUrl = fileUrl;
        //     existingStatus.ErrorMessage = errorMessage;
        //     existingStatus.UpdatedAt = DateTime.UtcNow;
        //     
        //     _cache.Set($"upload_{requestId}", existingStatus, TimeSpan.FromHours(24));
        // }
        // return Task.CompletedTask;
        return null;

    }

    // public Task<UploadStatusEntity?> GetUploadStatusAsync(Guid requestId, string userId)
    // {
    //     // _cache.TryGetValue($"upload_{requestId}", out UploadStatusEntity? status);
    //     // return Task.FromResult(status?.UserId == userId ? status : null);
    //     return null;
    //
    // }
}