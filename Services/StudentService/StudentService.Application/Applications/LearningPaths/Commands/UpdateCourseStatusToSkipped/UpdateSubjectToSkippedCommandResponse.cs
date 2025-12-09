using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateCourseStatusToSkipped;

public record UpdateSubjectToSkippedCommandResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}