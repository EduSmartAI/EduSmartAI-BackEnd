using BuildingBlocks.Messaging.Events.AIService.ModuleProgress;

namespace Course.Application.Interfaces
{
    public interface IVUserModuleProgressService
    {
        Task<UserCourseProgressDto> GetUserCourseProgressAsync(
            Guid courseId,
            Guid studentId,
            CancellationToken cancellationToken = default);
    }
}
