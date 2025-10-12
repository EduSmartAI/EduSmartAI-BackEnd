using AiService.Application.Interfaces;

namespace AiService.Application.Consumers.AiQuizEvaluates
{
	public class QuizEvaluableCreatedEventConsumer(IAiQuizEvaluatorService aiQuizEvaluatorService) : MassTransit.IConsumer<BuildingBlocks.Messaging.Events.QuizService.QuizEvaluableCreatedEvent>
	{
		public async Task Consume(MassTransit.ConsumeContext<BuildingBlocks.Messaging.Events.QuizService.QuizEvaluableCreatedEvent> context)
		{
			var evt = context.Message;
			await aiQuizEvaluatorService.EvaluateAsync(evt, context.CancellationToken);
		}
	}
}
