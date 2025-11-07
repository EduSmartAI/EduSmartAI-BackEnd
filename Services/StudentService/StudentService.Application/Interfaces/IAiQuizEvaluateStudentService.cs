using BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents;
using StudentService.Application.Applications.AiQuizEvaluates.Commands.CreateAiQuizEvaluate;
using StudentService.Application.Applications.Dashboards.Queries;
using StudentService.Application.Applications.Dashboards.Queries.GetOverviewCourseDashboard;

namespace StudentService.Application.Interfaces
{
    public interface IAiQuizEvaluateStudentService
    {
        Task<CreateAiQuizEvaluateResponse> CreateAiQuizEvaluate(AiEvaluationUpsertEvent aiEvaluationUpsertEvent, CancellationToken ct = default);
        Task<GetLatestModuleAiEvaluationsResponse> GetLatestModuleAiEvaluationsAsync(GetLatestModuleAiEvaluationsQuery request, CancellationToken cancellationToken);
        Task<GetLatestLessonAiEvaluationsResponse> GetLatestLessonAiEvaluationsAsync(GetLatestLessonAiEvaluationsQuery request, CancellationToken cancellationToken);
        Task<OverviewAiEvaluationResult> GetOverviewAiEvaludationAsync(Guid StudentId, Guid CourseId, CancellationToken cancellationToken);
        Task<string> InsertOverviewSummaryAsync(Guid studentId, Guid courseId, string markdownSummary, CancellationToken ct = default);
        Task<bool> UpdateModuleFeedbackAsync(Guid moduleId, Guid studentId, string markdown, CancellationToken cancellationToken);
    }
}
