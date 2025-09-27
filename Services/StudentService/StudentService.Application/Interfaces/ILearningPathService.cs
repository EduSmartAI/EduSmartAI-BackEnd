using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Applications.LearningPathsMajor.Commands.InsertLearningPathsMajor;

namespace StudentService.Application.Interfaces;

public interface ILearningPathService
{
    Task<LearningPathInsertResponse> InsertLearningPathAsync(LearningPathInsertCommand request, CancellationToken cancellationToken);
    Task<InsertLearningPathsMajorResponse> InsertLearningPathMajorCourseAsync(InsertLearningPathsMajorCommand request, CancellationToken cancellationToken);
}