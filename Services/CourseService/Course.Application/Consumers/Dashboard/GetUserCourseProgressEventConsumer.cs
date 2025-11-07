using BuildingBlocks.Messaging.Events.AIService.ModuleProgress;

namespace Course.Application.Consumers.Dashboard
{
    public class GetUserCourseProgressEventConsumer(
        IVUserModuleProgressService _vUserModuleProgressService
    ) : IConsumer<GetUserCourseProgressEvent>
    {
        /// <summary>
        /// Get and response to AiEvaluationService
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<GetUserCourseProgressEvent> context)
        {
            var msg = context.Message;

            var dto = await _vUserModuleProgressService.GetUserCourseProgressAsync(
                msg.CourseId,
                msg.StudentId,
                context.CancellationToken);

            await context.RespondAsync(new GetUserCourseProgressResponse
            {
                Response = dto
            });
        }
    }
}
