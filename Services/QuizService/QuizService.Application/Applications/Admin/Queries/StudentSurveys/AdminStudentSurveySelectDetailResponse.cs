using BaseService.Common.ApiEntities;
using QuizService.Application.Applications.StudentSurveys.Queries;

namespace QuizService.Application.Applications.Admin.Queries.StudentSurveys;

public record AdminStudentSurveySelectDetailResponse : AbstractApiResponse<StudentSurveySelectDetailResponseEntity>
{
    public override StudentSurveySelectDetailResponseEntity Response { get; set; } = null!;
}

