using AiService.Application.Features.AiSummary;
using BuildingBlocks.Messaging.Events.AIService.AiFeedback;
using MassTransit;
using MediatR;

namespace AiService.Application.Consumers.AiSummaryAndFeedback
{
    public class QuizAiFeedBackModuleEventConsumer(IMediator _mediator)
        : IConsumer<QuizAiFeedBackModuleEvent>
    {
        public async Task Consume(ConsumeContext<QuizAiFeedBackModuleEvent> context)
        {
            var message = context.Message;
            var request = new AiSummaryFeedbackModuleRequest
            {
                StudentId = message.StudentId,
                CourseId = message.CourseId,
                ModuleId = message.ModuleId
            };
            await _mediator.Send(request, context.CancellationToken);

            // Support synchronous request/response from other services
            if (context.RequestId.HasValue)
            {
                await context.RespondAsync(new QuizAiFeedBackModuleResponse(true));
            }
        }
    }
}
