using AiService.Application.Interfaces;
using MassTransit;

namespace AiService.Application.Consumers.AiQuizEvaluates
{
    public class QuizEvaluableCreatedEventConsumer(IAiQuizEvaluatorService aiQuizEvaluatorService) : IConsumer<BuildingBlocks.Messaging.Events.QuizService.QuizEvaluableCreatedEvent>
    {
        public async Task Consume(ConsumeContext<BuildingBlocks.Messaging.Events.QuizService.QuizEvaluableCreatedEvent> context)
        {
            var evt = context.Message;
            await aiQuizEvaluatorService.EvaluateAsync(evt, context.CancellationToken);
        }
    }
}
