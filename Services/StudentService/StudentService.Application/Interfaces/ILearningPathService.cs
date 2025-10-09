using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Applications.LearningPaths.Commands.InsertInternal;
using StudentService.Application.Applications.LearningPaths.Queries;
using StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths;
using StudentService.Application.Applications.LearningPathsMajor.Commands.InsertLearningPathsMajor;

namespace StudentService.Application.Interfaces;

public interface ILearningPathService
{
    Task<LearningPathInsertResponse> InsertLearningPathAsync(LearningPathInsertCommand request, CancellationToken cancellationToken);

    Task<InsertLearningPathsMajorResponse> InsertLearningPathMajorCourseAsync(InsertLearningPathsMajorCommand request, CancellationToken cancellationToken);

    Task<LearningPathMajorInternalInsertResponse> InsertLearningPathMajorAsync(LearningPathMajorInsertCommand request, CancellationToken cancellationToken = default);
    Task<LearningPathSelectResponse> GetLearningPathById(LearningPathSelectsQuery query, CancellationToken cancellationToken = default);
    Task<InsertInternalLearningPathResponse> InsertInternalMajorAndCourse(InsertInternalLearningPathCommand request, CancellationToken cancellationToken);
}