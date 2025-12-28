using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.StudentTests.Commands;

public class StudentTestInsertCommandHandler : ICommandHandler<StudentTestInsertCommand, StudentTestInsertResponse>
{
    private readonly IStudentTestService _studentTestService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentTestService"></param>
    public StudentTestInsertCommandHandler(IStudentTestService studentTestService)
    {
        _studentTestService = studentTestService;
    }

    /// <summary>
    /// Handle insert student test
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentTestInsertResponse> Handle(StudentTestInsertCommand request, CancellationToken cancellationToken)
    {
        return await _studentTestService.InsertStudentTestAsync(request, cancellationToken);
    }
}