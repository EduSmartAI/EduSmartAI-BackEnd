using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningGoals.Commands;

public record LearningGoalInsertCommand : ICommand<LearningGoalInsertResponse>
{
    public string GoalName { get; set; } = null!;

    public string? Description { get; set; }
    
    [Range(1,4, ErrorMessage = "LearningGoalType must be between 1 and 4.")]
    public short LearningGoalType { get; set; }
}