using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningGoals.Commands;

public record LearningGoalDeleteCommand : ICommand<LearningGoalDeleteResponse>
{
    [Required(ErrorMessage = "GoalId is required")]
    public Guid GoalId { get; set; }
}

