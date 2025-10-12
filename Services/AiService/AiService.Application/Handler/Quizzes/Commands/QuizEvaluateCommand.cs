using AiService.Application.DTOs;
using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.QuizService;

namespace AiService.Application.Handler.Quizzes.Commands
{
	public record QuizEvaluateCommand(QuizEvaluableCreatedEvent QuizEvaluableCreatedEvent) : ICommand<QuizEvaluateResponse>;

	public record QuizEvaluateResponse : AbstractApiResponse<AiEvaluationDto>
	{
		public override AiEvaluationDto Response { get; set; } = default!;
	}
}
