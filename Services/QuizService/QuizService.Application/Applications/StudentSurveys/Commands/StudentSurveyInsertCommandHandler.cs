using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.StudentSurveys.Commands;

public class StudentSurveyInsertCommandHandler : ICommandHandler<StudentSurveyInsertCommand, StudentSurveyInsertResponse>
{
    private readonly IStudentSurveyService _studentQuizService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentQuizService"></param>
    public StudentSurveyInsertCommandHandler(IStudentSurveyService studentQuizService)
    {
        _studentQuizService = studentQuizService;
    }

    /// <summary>
    /// Handle insert student survey
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentSurveyInsertResponse> Handle(StudentSurveyInsertCommand request, CancellationToken cancellationToken)
    {
        return await _studentQuizService.InsertStudentSurveyAsync(request, cancellationToken);
    }
}