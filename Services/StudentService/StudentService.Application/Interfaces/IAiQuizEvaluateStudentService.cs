using BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents;
using StudentService.Application.Applications.AiQuizEvaluates.Commands.CreateAiQuizEvaluate;

namespace StudentService.Application.Interfaces
{
	public interface IAiQuizEvaluateStudentService
	{
		Task<CreateAiQuizEvaluateResponse> CreateAiQuizEvaluate(AiEvaluationUpsertEvent aiEvaluationUpsertEvent, CancellationToken ct = default);
	}
}
