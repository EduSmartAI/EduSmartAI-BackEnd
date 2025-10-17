using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.QuizCourses.Commands;

/// <summary>
/// Command to delete questions from an existing quiz (soft delete - set IsActive = false)
/// </summary>
public class QuizCourseDeleteQuestionsCommand : ICommand<QuizCourseDeleteQuestionsResponse>
{
    [Required(ErrorMessage = "QuizId is required")]
    public Guid QuizId { get; set; }
    
    [Required(ErrorMessage = "QuestionIds are required")]
    [MinLength(1, ErrorMessage = "At least one question ID is required")]
    public List<Guid> QuestionIds { get; set; } = null!;
}

