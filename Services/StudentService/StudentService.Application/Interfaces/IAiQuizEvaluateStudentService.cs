using BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents;
using StudentService.Application.Applications.AiQuizEvaluates.Commands.CreateAiQuizEvaluate;
using StudentService.Application.Applications.Dashboards.Queries;

namespace StudentService.Application.Interfaces
{
	public interface IAiQuizEvaluateStudentService
	{
		Task<CreateAiQuizEvaluateResponse> CreateAiQuizEvaluate(AiEvaluationUpsertEvent aiEvaluationUpsertEvent, CancellationToken ct = default);
		Task<GetLatestModuleAiEvaluationsResponse> GetLatestModuleAiEvaluationsAsync(GetLatestModuleAiEvaluationsQuery request, CancellationToken cancellationToken);
		Task<GetLatestLessonAiEvaluationsResponse> GetLatestLessonAiEvaluationsAsync(GetLatestLessonAiEvaluationsQuery request, CancellationToken cancellationToken);
	}
}
