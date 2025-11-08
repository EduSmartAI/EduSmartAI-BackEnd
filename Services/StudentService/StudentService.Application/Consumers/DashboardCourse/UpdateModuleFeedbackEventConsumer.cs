using BuildingBlocks.Messaging.Events.AIService.AiFeedback;
using MassTransit;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers.DashboardCourse
{
    public class UpdateModuleFeedbackEventConsumer(IAiQuizEvaluateStudentService _aiQuizEvaluateStudentService) : IConsumer<UpdateModuleFeedbackEvent>
    {
        public async Task Consume(ConsumeContext<UpdateModuleFeedbackEvent> context)
        {
            var msg = context.Message;

            var updated = await _aiQuizEvaluateStudentService.UpdateModuleFeedbackAsync(
                msg.ModuleId,
                msg.StudentId,
                msg.markdownFeedBack,
                context.CancellationToken);

            await context.RespondAsync(new UpdateModuleFeedbackResponse
            {
                Success = updated,
                Response = updated
                    ? "Updated module feedback successfully"
                    : "No module evaluation found to update"
            });
        }
    }
}
