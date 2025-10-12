using MassTransit;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers
{
	public class UpsertAiQuizEvaluationEventConsumer(IAiQuizEvaluateStudentService aiQuizEvaluateStudentService) : IConsumer<BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents.AiEvaluationUpsertEvent>
	{
		public async Task Consume(ConsumeContext<BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents.AiEvaluationUpsertEvent> context)
		{
			var evt = context.Message;
			await aiQuizEvaluateStudentService.CreateAiQuizEvaluate(evt, context.CancellationToken);
		}
	}
}
