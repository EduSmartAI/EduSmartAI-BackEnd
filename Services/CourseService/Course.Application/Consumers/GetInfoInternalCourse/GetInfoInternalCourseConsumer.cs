using BuildingBlocks.Messaging.Events.StudentService.GetInfoInternalCourse;

namespace Course.Application.Consumers.GetInfoInternalCourse
{
    public class GetInfoInternalCourseConsumer(ICourseService _courseService) : IConsumer<GetInfoInternalCourseEvents>
    {
        public async Task Consume(ConsumeContext<GetInfoInternalCourseEvents> context)
        {
            var message = context.Message;
            var response = await _courseService.GetAllViewCourseByListId(message, context.CancellationToken);
            await context.RespondAsync<GetInfoInternalCourseResponse>(response);

        }
    }
}
