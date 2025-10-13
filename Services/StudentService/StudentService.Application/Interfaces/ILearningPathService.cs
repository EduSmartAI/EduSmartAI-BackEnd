using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateCourses;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateReadModel;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateStatusLearningPath;
using StudentService.Application.Applications.LearningPaths.Queries;
using StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths;
using StudentService.Application.Applications.LearningPathsMajor.Commands.InsertBatchLearningPathsMajor;
using StudentService.Application.Applications.LearningPathsMajor.Commands.InsertLearningPathsMajor;

namespace StudentService.Application.Interfaces;

public interface ILearningPathService
{
    Task<LearningPathInsertResponse> InsertLearningPathAsync(LearningPathInsertCommand request, CancellationToken cancellationToken);

    Task<InsertLearningPathsMajorResponse> InsertLearningPathMajorCourseAsync(InsertLearningPathsMajorCommand request, CancellationToken cancellationToken);

    Task<LearningPathMajorInternalInsertResponse> InsertLearningPathMajorAsync(LearningPathMajorInsertCommand request, CancellationToken cancellationToken = default);

    Task<InsertBatchLearningPathsMajorResponse> InsertBatchLearningPathMajorCourseAsync(InsertBatchLearningPathsMajorCommand request, CancellationToken cancellationToken);

    Task<bool> UpdateLearningPathStatusAsync(Guid learningPathId, CancellationToken contextCancellationToken);

    Task<LearningPathSelectResponse> GetLearningPathById(LearningPathSelectsQuery query, CancellationToken cancellationToken = default);
    Task<UpdateStatusLearningPathResponse> UpdateStatusLearningPathByIdAndSortPosition(UpdateStatusLearningPathCommand request, CancellationToken cancellationToken);
    Task<UpdateReadModelLearningPathResponse> UpdateStatusLearningPathReadModelByIdAndSortPosition(UpdateReadModelLearningPathCommand request, CancellationToken cancellationToken);

    Task<LearningPathCourseUpdateResponse> UpdateLearningPathCoursesAsync(LearningPathCourseUpdateCommand request, CancellationToken cancellationToken);
}