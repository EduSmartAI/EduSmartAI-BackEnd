using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningGoals.Commands;

public record LearningGoalInsertCommand : ICommand<LearningGoalInsertResponse>
{
    public string GoalName { get; set; } = null!;

    public string? Description { get; set; }
}