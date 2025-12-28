using BaseService.Application.Common;
using BaseService.Common.ApiEntities;
using BaseService.Common.Utils.Const;

namespace StudentService.Application.Applications.LearningGoals.Queries;

public record AdminLearningGoalsSelectResponse : AbstractApiResponse<PagedResult<AdminLearningGoalItem>>
{
    public override PagedResult<AdminLearningGoalItem> Response { get; set; } = new();
}

public class AdminLearningGoalItem
{
    public Guid GoalId { get; set; }
    
    public string GoalName { get; set; } = null!;
    
    public string? Description { get; set; }
    
    public short LearningGoalType { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

