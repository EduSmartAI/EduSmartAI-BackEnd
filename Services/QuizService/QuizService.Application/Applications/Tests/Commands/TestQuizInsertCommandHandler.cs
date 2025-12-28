using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Tests.Commands;

public class TestQuizInsertCommandHandler(ITestService testService) : ICommandHandler<TestQuizInsertCommand, TestQuizInsertResponse>
{
    public async Task<TestQuizInsertResponse> Handle(TestQuizInsertCommand request, CancellationToken cancellationToken)
    {
        return await testService.InsertTestQuizAsync(request, cancellationToken);
    }
}

