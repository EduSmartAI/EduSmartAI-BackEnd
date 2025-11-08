using BuildingBlocks.Messaging.Events.AIService.ModuleProgress;
using BuildingBlocks.Messaging.Events.StudentService.GetInfoEvaluation;
using StudentService.Application.Applications.Dashboards.Commands;

namespace StudentService.Application.Interfaces
{
    public interface IAiEvaluationService
    {
        Task<GetInfoEvaluationEventResponse> GetAllEvaluationByCourseId(Guid studentId, Guid courseId, CancellationToken cancellationToken);
        Task<ModuleProgressDto> GetModuleProgressAsync(
            Guid studentId,
            Guid courseId,
            Guid moduleId,
            CancellationToken cancellationToken);
        Task<SearchAiRecommendResponse> GenAndInsertImprovement(Guid ImprovementId, CancellationToken cancellationToken);

    }
}
