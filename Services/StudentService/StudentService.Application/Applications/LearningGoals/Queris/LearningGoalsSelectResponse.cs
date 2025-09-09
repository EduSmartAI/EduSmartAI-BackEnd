using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningGoals.Queris;

public record LearningGoalsSelectResponse : AbstractApiResponse<List<LearningGoalsSelectResponseEntity>>
{
    public override List<LearningGoalsSelectResponseEntity> Response { get; set; }
}

public class LearningGoalsSelectResponseEntity
{
    public Guid GoalId { get; set; }

    public string GoalName { get; set; } = null!;

    public string? Description { get; set; }
}