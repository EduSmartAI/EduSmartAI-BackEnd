using BuildingBlocks.Messaging.Events.StudentService.GetOverviewCourse;

namespace Course.Application.Interfaces
{
    public interface IOverviewCourseService
    {
        Task<GetOverviewCourseResponse> GetOverviewCourseByIds(GetOverviewCourseEvents events, CancellationToken ct = default);
        Task<CoursePaceStatsDto?> GetCoursePaceStatsAsync(Guid courseId, Guid studentId, CancellationToken ct = default);
    }
}
