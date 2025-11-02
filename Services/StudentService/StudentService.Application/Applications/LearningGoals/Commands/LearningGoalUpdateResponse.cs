using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningGoals.Commands;

public record LearningGoalUpdateResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; } = null!;
}

