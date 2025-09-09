using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.Students.Commands.Inserts;

public record StudentInsertProfileCommand : ICommand<StudentInsertProfileResponse>
{
    [Required(ErrorMessage = "MajorId is required")]
    public Guid MajorId { get; set; }
    
    [Required(ErrorMessage = "SemesterId is required")]
    public int SemesterId { get; set; }
    
    [Required(ErrorMessage = "LearningGoals is required")]
    public List<LearningGoals> LearningGoals { get; set; } = null!;
}

public class LearningGoals
{
    [Required(ErrorMessage = "LearningGoalId is required")]
    public Guid LearningGoalId { get; set; }
}
