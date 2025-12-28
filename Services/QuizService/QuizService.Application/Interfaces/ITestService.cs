using QuizService.Application.Applications.Tests.Commands;
using QuizService.Application.Applications.Tests.Queries;

namespace QuizService.Application.Interfaces;

public interface ITestService
{
    Task<TestInsertResponse> InsertTestAsync(TestInsertCommand request, CancellationToken cancellationToken);
    
    Task<TestQuizInsertResponse> InsertTestQuizAsync(TestQuizInsertCommand request, CancellationToken cancellationToken);
    
    Task<TestQuizDeleteResponse> DeleteTestQuizAsync(TestQuizDeleteCommand request, CancellationToken cancellationToken);
    
    Task<TestQuizQuestionsInsertResponse> InsertTestQuizQuestionsAsync(TestQuizQuestionsInsertCommand request, CancellationToken cancellationToken);
    
    Task<TestQuizQuestionsDeleteResponse> DeleteTestQuizQuestionsAsync(TestQuizQuestionsDeleteCommand request, CancellationToken cancellationToken);
    
    Task<TestSelectResponse> SelectTestAsync(TestSelectQuery request);
}