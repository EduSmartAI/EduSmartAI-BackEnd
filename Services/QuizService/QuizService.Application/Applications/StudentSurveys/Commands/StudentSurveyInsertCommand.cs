using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.StudentSurveys.Commands;

public record StudentSurveyInsertCommand : ICommand<StudentSurveyInsertResponse>
{
    public List<StudentSurveyInsertRequest> StudentSurveys { get; set; }
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