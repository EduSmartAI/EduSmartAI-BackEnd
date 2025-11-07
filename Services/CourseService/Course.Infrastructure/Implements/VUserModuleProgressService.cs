using BuildingBlocks.Messaging.Events.AIService.ModuleProgress;

namespace Course.Infrastructure.Implements
{
    public class VUserModuleProgressService(
        ICommandRepository<VUserCourseProgress> _courseRepository
    ) : IVUserModuleProgressService
    {
        /// <summary>
        /// Get module progress
        /// </summary>
        /// <param name="courseId"></param>
        /// <param name="studentId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<UserCourseProgressDto> GetUserCourseProgressAsync(
            Guid courseId,
            Guid studentId,
            CancellationToken cancellationToken = default)
        {
            var query = _courseRepository.Find(
                x => x.UserId == studentId && x.CourseId == courseId,
                isTracking: false,
                cancellationToken: cancellationToken);

            var progress = await query.FirstOrDefaultAsync(cancellationToken);

            var dto = new UserCourseProgressDto();

            if (progress is not null)
            {
                dto.LessonsTotal = progress.LessonsTotal ?? 0;
                dto.LessonsCompleted = progress.LessonsCompleted ?? 0;
                dto.PercentCompleted = progress.PercentCompleted.HasValue
                    ? (double)progress.PercentCompleted.Value
                    : 0d;
            }

            return dto;
        }
    }
}
