using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateCourses;

/// <summary>
/// Response for learning path course update
/// </summary>
public record LearningPathCourseUpdateResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}