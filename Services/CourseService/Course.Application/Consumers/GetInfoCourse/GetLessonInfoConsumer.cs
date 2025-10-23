using BuildingBlocks.Messaging.Events.AIService.GetLessonInfoEvent;

namespace Course.Application.Consumers.GetInfoCourse
{
    public class GetLessonInfoConsumer(IVwCourseInforService _courseService) : IConsumer<GetLessonInfoEvent>
    {
        public async Task Consume(ConsumeContext<GetLessonInfoEvent> context)
        {
            var message = context.Message;
            var response = await _courseService.GetAllInforLessonById(message, context.CancellationToken);
            await context.RespondAsync<GetLessonInfoResponse>(response);
        }
    }
}
