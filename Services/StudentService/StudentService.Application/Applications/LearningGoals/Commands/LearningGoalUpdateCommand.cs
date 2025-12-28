using System.ComponentModel.DataAnnotations;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningGoals.Commands;

public record LearningGoalUpdateCommand : ICommand<LearningGoalUpdateResponse>
{
    [Required(ErrorMessage = "GoalId is required")]
    public Guid GoalId { get; set; }
    
    [Required(ErrorMessage = "GoalName is required")]
    public string GoalName { get; set; } = null!;

    public string? Description { get; set; }
    
    [Required(ErrorMessage = "LearningGoalType is required")]
    public ConstantEnum.LearningGoalType LearningGoalType { get; set; }
}