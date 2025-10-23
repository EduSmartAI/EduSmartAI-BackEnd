using BuildingBlocks.Messaging.Events.AIService.GetLessonInfoEvent;

namespace Course.Application.Interfaces
{
    public interface IVwCourseInforService
    {
        Task<GetLessonInfoResponse> GetAllInforLessonById(GetLessonInfoEvent request, CancellationToken ct = default);

    }
}
