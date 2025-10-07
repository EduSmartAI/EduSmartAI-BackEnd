using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Applications.LearningPathsMajor.Commands.InsertLearningPathsMajor;
using StudentService.Application.Applications.LearningPathsMajor.Commands.InsertBatchLearningPathsMajor;

namespace StudentService.Application.Interfaces;

public interface ILearningPathService
{
    Task<LearningPathInsertResponse> InsertLearningPathAsync(LearningPathInsertCommand request, CancellationToken cancellationToken);
    
    Task<InsertLearningPathsMajorResponse> InsertLearningPathMajorCourseAsync(InsertLearningPathsMajorCommand request, CancellationToken cancellationToken);
    
    Task<LearningPathMajorInternalInsertResponse> InsertLearningPathMajorAsync(LearningPathMajorInsertCommand request, CancellationToken cancellationToken = default);
    
    Task<InsertBatchLearningPathsMajorResponse> InsertBatchLearningPathMajorCourseAsync(InsertBatchLearningPathsMajorCommand request, CancellationToken cancellationToken);
    
    Task<bool> UpdateLearningPathStatusAsync(Guid learningPathId, CancellationToken contextCancellationToken);
}