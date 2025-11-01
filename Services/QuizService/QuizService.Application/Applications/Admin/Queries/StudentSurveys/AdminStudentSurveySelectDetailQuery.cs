using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Admin.Queries.StudentSurveys;

public record AdminStudentSurveySelectDetailQuery : IQuery<AdminStudentSurveySelectDetailResponse>
{
    [Required(ErrorMessage = "StudentSurveyId is required")]
    public Guid StudentSurveyId { get; set; }
}