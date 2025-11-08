using BuildingBlocks.Messaging.Events.AIService.ModuleProgress;
using MassTransit;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers.DashboardCourse
{
    public class GetModuleProgressEventsConsumer(
        IAiEvaluationService _aiEvaluationService
    ) : IConsumer<GetModuleProgressEvents>
    {
        /// <summary>
        /// Get and response module progress to AiSummaryFeedbackModuleHandler 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<GetModuleProgressEvents> context)
        {
            var msg = context.Message;

            var dto = await _aiEvaluationService.GetModuleProgressAsync(
                msg.StudentId,
                msg.CourseId,
                msg.ModuleId,
                context.CancellationToken);

            await context.RespondAsync(new GetModuleProgresssRepsonse
            {
                Response = dto
            });
        }
    }
}
