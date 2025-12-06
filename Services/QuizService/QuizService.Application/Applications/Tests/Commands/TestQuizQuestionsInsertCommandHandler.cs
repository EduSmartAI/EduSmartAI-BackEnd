using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Tests.Commands;

public class TestQuizQuestionsInsertCommandHandler(ITestService testService) : ICommandHandler<TestQuizQuestionsInsertCommand, TestQuizQuestionsInsertResponse>
{
    public async Task<TestQuizQuestionsInsertResponse> Handle(TestQuizQuestionsInsertCommand request, CancellationToken cancellationToken)
    {
        return await testService.InsertTestQuizQuestionsAsync(request, cancellationToken);
    }
}

