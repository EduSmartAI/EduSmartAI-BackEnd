using BuildingBlocks.Messaging.Events.AIService.GetLessonInfoEvent;
using BuildingBlocks.Messaging.Events.StudentService.GetAllDetailCourse;

namespace Course.Application.Interfaces
{
    public interface IVwCourseInforService
    {
        Task<GetLessonInfoResponse> GetAllInforLessonById(GetLessonInfoEvent request, CancellationToken ct = default);
        Task<CourseHierInfoDto> GetAllInfoByCourseId(Guid courseId, Guid? studentId = null, CancellationToken ct = default);

    }
}
