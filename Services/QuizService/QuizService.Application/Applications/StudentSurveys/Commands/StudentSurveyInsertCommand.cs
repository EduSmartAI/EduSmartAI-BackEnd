using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.StudentSurveys.Commands;

public record StudentSurveyInsertCommand : ICommand<StudentSurveyInsertResponse>
{
    [Required(ErrorMessage = "StudentInformation is required")]
    public StudentInformation StudentInformation { get; set; } = null!;

    [Required(ErrorMessage = "StudentSurveys is required")]
    public List<StudentSurveyInsertRequest> StudentSurveys { get; set; } = null!;
}

public class StudentInformation
{
    [Required(ErrorMessage = "MajorId is required")]
    public Guid MajorId { get; set; }

    [Required(ErrorMessage = "SemesterId is required")]
    public Guid SemesterId { get; set; }

    [Required(ErrorMessage = "TechnologyIds is required")]
    public List<Technology> Technologies { get; set; } = null!;

    [Required(ErrorMessage = "LearningGoal is required")]
    public LearningGoal LearningGoal { get; set; } = null!;
}

public class Technology
{
    public Guid TechnologyId { get; set; }
    
    public string TechnologyName { get; set; } = null!;

    [Range(1, 4, ErrorMessage = "TechnologyType must be between 1 and 4")]
    public short TechnologyType { get; set; }
}

public class LearningGoal
{
    public Guid LearningGoalId { get; set; }
    
    public short LearningGoalType { get; set; }
}

public record StudentSurveyInsertRequest
{
    [Required(ErrorMessage = "SurveyId is required")]
    public Guid SurveyId { get; set; }
    
    [Required(ErrorMessage = "SurveyCode is required")]
    public string SurveyCode { get; set; } = null!;

    [Required(ErrorMessage = "Answers is required")]
    public List<StudentQuizAnswerInsertRequest> Answers { get; set; } = null!;
}

public record StudentQuizAnswerInsertRequest
{
    [Required(ErrorMessage = "QuestionId is required")]
    public Guid QuestionId { get; set; }

    public Guid AnswerId { get; set; }
}