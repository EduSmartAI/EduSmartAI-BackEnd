using AiService.Application.Handler.Quizzes.Commands;
using BuildingBlocks.Messaging.Events.QuizService;

namespace AiService.Application.Interfaces
{
	public interface IAiQuizEvaluatorService
	{
		Task<QuizEvaluateResponse> EvaluateAsync(QuizEvaluableCreatedEvent evt, CancellationToken ct);
	}
}
