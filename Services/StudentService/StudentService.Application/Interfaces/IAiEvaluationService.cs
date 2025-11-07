using BuildingBlocks.Messaging.Events.AIService.ModuleProgress;
using BuildingBlocks.Messaging.Events.StudentService.GetInfoEvaluation;

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
    }
}
