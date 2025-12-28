using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.StudentTests.Queries;

public class StudentTestSelectQueryHandler : IQueryHandler<StudentTestSelectQuery, StudentTestSelectResponse>
{
    private readonly IStudentTestService _studentTestService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentTestService"></param>
    public StudentTestSelectQueryHandler(IStudentTestService studentTestService)
    {
        _studentTestService = studentTestService;
    }

    public async Task<StudentTestSelectResponse> Handle(StudentTestSelectQuery request, CancellationToken cancellationToken)
    {
        return await _studentTestService.SelectStudentTestAsync(request);
    }
}