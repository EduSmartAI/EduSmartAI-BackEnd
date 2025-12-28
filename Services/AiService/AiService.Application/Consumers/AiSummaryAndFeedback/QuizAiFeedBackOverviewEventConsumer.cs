using AiService.Application.Features.AiSummary;
using BuildingBlocks.Messaging.Events.AIService.AiFeedback;
using MassTransit;
using MediatR;

namespace AiService.Application.Consumers.AiSummaryAndFeedback
{
    public class QuizAiFeedBackOverviewEventConsumer(IMediator _mediator)
        : IConsumer<QuizAiFeedBackOverviewEvent>
    {
        public async Task Consume(ConsumeContext<QuizAiFeedBackOverviewEvent> context)
        {
            var message = context.Message;

            var request = new AiSummaryRequest
            {
                StudentId = message.StudentId,
                CourseId = message.CourseId
            };
            await _mediator.Send(request, context.CancellationToken);

            // Support synchronous request/response from other services
            if (context.RequestId.HasValue)
            {
                await context.RespondAsync(new QuizAiFeedBackOverviewResponse(true));
            }
        }
    }
}
