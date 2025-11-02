using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningGoals.Commands;

public record LearningGoalDeleteResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; } = null!;
}


