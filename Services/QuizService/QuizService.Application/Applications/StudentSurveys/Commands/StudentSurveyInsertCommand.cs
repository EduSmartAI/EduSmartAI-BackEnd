using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.StudentSurveys.Commands;

public record StudentSurveyInsertCommand : ICommand<StudentSurveyInsertResponse>
{
    [Required(ErrorMessage = "StudentInformation is required")]
    public StudentInformation StudentInformation { get; set; }
    
    [Required(ErrorMessage = "StudentSurveys is required")]
    public List<StudentSurveyInsertRequest> StudentSurveys { get; set; }
}

public class StudentInformation
{
    [Required(ErrorMessage = "MajorId is required")]
    public Guid MajorId { get; set; }

    [Required(ErrorMessage = "SemesterId is required")]
    public Guid SemesterId { get; set; }

    [Required(ErrorMessage = "TechnologyIds is required")]
    public List<Guid> TechnologyIds { get; set; }

    [Required(ErrorMessage = "LearningGoalIds is required")]
    public List<Guid> LearningGoalIds { get; set; }
}

public record StudentSurveyInsertRequest
{
    [Required(ErrorMessage = "SurveyId is required")]
    public Guid SurveyId { get; set; }

    [Required(ErrorMessage = "Answers is required")]
    public List<StudentQuizAnswerInsertRequest> Answers { get; set; }
}

public record StudentQuizAnswerInsertRequest
{
    [Required(ErrorMessage = "QuestionId is required")]
    public Guid QuestionId { get; set; }

    public Guid? AnswerId { get; set; }

    public string? AnswerText { get; set; }
}