using QuizService.Application.Applications.Tests.Commands;
using QuizService.Application.Applications.Tests.Queries;

namespace QuizService.Application.Interfaces;

public interface ITestService
{
    Task<TestInsertResponse> InsertTestAsync(TestInsertCommand request, CancellationToken cancellationToken);
    
    Task<TestSelectResponse> SelectTestAsync(TestSelectQuery request);
}