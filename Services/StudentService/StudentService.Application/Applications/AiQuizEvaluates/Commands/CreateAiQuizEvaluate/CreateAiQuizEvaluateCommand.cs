using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents;

namespace StudentService.Application.Applications.AiQuizEvaluates.Commands.CreateAiQuizEvaluate
{
	public record CreateAiQuizEvaluateCommand(AiEvaluationUpsertEvent AiEvaluationUpsertEvent) : ICommand<CreateAiQuizEvaluateResponse>;

	public record CreateAiQuizEvaluateResponse : AbstractApiResponse<string>
	{
		public override string Response { get; set; } = default!;
	}
}
