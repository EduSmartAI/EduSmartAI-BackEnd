using AiService.Application.Interfaces;
using BuildingBlocks.CQRS;

namespace AiService.Application.Handler.Quizzes.Commands
{
	public class QuizEvaluateHandler(IAiQuizEvaluatorService _aiQuizEvaluatorService)
		: ICommandHandler<QuizEvaluateCommand, QuizEvaluateResponse>
	{
		public async Task<QuizEvaluateResponse> Handle(QuizEvaluateCommand request, CancellationToken cancellationToken)
		{
			return await _aiQuizEvaluatorService.EvaluateAsync(request.QuizEvaluableCreatedEvent, cancellationToken);
		}
	}
}
