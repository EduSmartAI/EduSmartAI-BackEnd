using System.ComponentModel.DataAnnotations;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningGoals.Commands;

public record LearningGoalInsertCommand : ICommand<LearningGoalInsertResponse>
{
    public string GoalName { get; set; } = null!;

    public string? Description { get; set; }
    
    public ConstantEnum.LearningGoalType LearningGoalType { get; set; }
}