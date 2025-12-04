using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateCourses;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateCourseStatusToSkipped;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateLearningPathStatus;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateReadModel;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateStatusLearningPath;
using StudentService.Application.Applications.LearningPaths.Queries;
using StudentService.Application.Applications.LearningPaths.Queries.SelectAllLearningPath;
using StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths;
using StudentService.Application.Applications.LearningPathsMajor.Commands.InsertBatchLearningPathsMajor;
using StudentService.Application.Applications.LearningPathsMajor.Commands.InsertLearningPathsMajor;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace StudentService.Application.Interfaces;

public interface ILearningPathService
{
    Task<LearningPathInsertResponse> InsertLearningPathAsync(LearningPathInsertCommand request, CancellationToken cancellationToken);

    Task<LearningPathRenameResponse> RenameLearningPathAsync(LearningPathRenameCommand request, CancellationToken cancellationToken);

    Task<InsertLearningPathsMajorResponse> InsertLearningPathMajorCourseAsync(InsertLearningPathsMajorCommand request, CancellationToken cancellationToken);

    Task<LearningPathMajorInternalInsertResponse> InsertLearningPathMajorAsync(LearningPathMajorInsertCommand request, CancellationToken cancellationToken = default);

    Task<InsertBatchLearningPathsMajorResponse> InsertBatchLearningPathMajorCourseAsync(InsertBatchLearningPathsMajorCommand request, CancellationToken cancellationToken);

    Task<bool> UpdateLearningPathStatusAsync(Guid learningPathId, CancellationToken contextCancellationToken);

    Task<LearningPathSelectResponse> GetLearningPathById(
        LearningPathSelectsQuery query,
        Guid userId,
        bool bypassCache = false,
        CancellationToken cancellationToken = default);

    Task<UpdateStatusLearningPathResponse> UpdateStatusLearningPathByIdAndSortPosition(UpdateStatusLearningPathCommand request, CancellationToken cancellationToken);

    Task<UpdateReadModelLearningPathResponse> UpdateStatusLearningPathReadModelByIdAndSortPosition(UpdateReadModelLearningPathCommand request, CancellationToken cancellationToken);

    Task<SelectAllLearningPathResponse> GetAllLearningPath(SelectAllLearningPathQuery query, Guid userId, CancellationToken cancellationToken = default);

    Task<LearningPathCourseUpdateResponse> UpdateLearningPathCoursesAsync(LearningPathCourseUpdateCommand request, CancellationToken cancellationToken);

    Task<UpdateCourseStatusToSkippedResponse> UpdateCourseStatusToSkippedAsync(UpdateCourseStatusToSkippedCommand request, Guid userId, string email, CancellationToken cancellationToken);

    Task<UpdateCourseStatusToSkippedResponse> UpdateCourseStatusToSkippedBySubjectAsync(Guid userId, Guid learningPathId, string subjectCode, string email, CancellationToken cancellationToken);

    Task UpdateCourseStatusForUserAsync(Guid userId, Guid courseId, short status, CancellationToken cancellationToken = default);

    Task<UpdateLearningPathStatusResponse> UpdateLearningPathStatusAsync(Guid learningPathId, LearningPathStatus newStatus, CancellationToken ct = default);
}