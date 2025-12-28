using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.StudentSurveys.Queries;

public class StudentSurveySelectDetailQueryHandler : IQueryHandler<StudentSurveySelectDetailQuery, StudentSurveySelectDetailResponse>
{
    private readonly IStudentSurveyService _studentSurveyService;

    public StudentSurveySelectDetailQueryHandler(IStudentSurveyService studentSurveyService)
    {
        _studentSurveyService = studentSurveyService;
    }

    public async Task<StudentSurveySelectDetailResponse> Handle(StudentSurveySelectDetailQuery request, CancellationToken cancellationToken)
    {
        return await _studentSurveyService.SelectStudentSurveyDetailAsync(request);
    }
}

