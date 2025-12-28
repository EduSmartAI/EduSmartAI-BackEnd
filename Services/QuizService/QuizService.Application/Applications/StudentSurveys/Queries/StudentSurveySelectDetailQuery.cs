using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.StudentSurveys.Queries;

public record StudentSurveySelectDetailQuery : IQuery<StudentSurveySelectDetailResponse>
{
    [Required(ErrorMessage = "StudentSurveyId is required")]
    public Guid StudentSurveyId { get; set; }
}