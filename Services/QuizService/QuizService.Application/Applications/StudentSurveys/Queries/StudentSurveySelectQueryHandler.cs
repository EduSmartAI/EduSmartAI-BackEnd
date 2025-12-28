using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.StudentSurveys.Queries;

public class StudentSurveySelectQueryHandler : IQueryHandler<StudentSurveySelectQuery, StudentSurveySelectResponse>
{
    private readonly IStudentSurveyService _studentQuizService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentQuizService"></param>
    public StudentSurveySelectQueryHandler(IStudentSurveyService studentQuizService)
    {
        _studentQuizService = studentQuizService;
    }

    /// <summary>
    /// Handle student survey select query
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentSurveySelectResponse> Handle(StudentSurveySelectQuery request, CancellationToken cancellationToken)
    {
        return await _studentQuizService.SelectStudentSurveyAsync(request);
    }
}