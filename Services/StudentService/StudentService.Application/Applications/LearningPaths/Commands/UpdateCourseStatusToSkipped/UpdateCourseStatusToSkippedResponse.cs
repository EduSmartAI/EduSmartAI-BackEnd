using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateCourseStatusToSkipped;

public record UpdateCourseStatusToSkippedResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}

