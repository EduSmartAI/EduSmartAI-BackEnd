using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Admin.Queries.StudentSurveys;

public class AdminStudentSurveySelectDetailQueryHandler : IQueryHandler<AdminStudentSurveySelectDetailQuery, AdminStudentSurveySelectDetailResponse>
{
    private readonly IStudentSurveyService _studentSurveyService;

    public AdminStudentSurveySelectDetailQueryHandler(IStudentSurveyService studentSurveyService)
    {
        _studentSurveyService = studentSurveyService;
    }

    public async Task<AdminStudentSurveySelectDetailResponse> Handle(AdminStudentSurveySelectDetailQuery request, CancellationToken cancellationToken)
    {
        return await _studentSurveyService.SelectAdminStudentSurveyDetailAsync(request, cancellationToken);
    }
}


