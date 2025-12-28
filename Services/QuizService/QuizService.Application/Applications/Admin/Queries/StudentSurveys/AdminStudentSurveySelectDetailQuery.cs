using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Admin.Queries.StudentSurveys;

public class AdminStudentSurveySelectDetailQuery : IQuery<AdminStudentSurveySelectDetailResponse>
{
    [Required(ErrorMessage = "StudentQuizId is required.")]
    public Guid StudentQuizId { get; set; }
}

