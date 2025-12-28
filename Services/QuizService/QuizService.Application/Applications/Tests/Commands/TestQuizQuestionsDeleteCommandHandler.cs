using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Tests.Commands;

public class TestQuizQuestionsDeleteCommandHandler(ITestService testService) : ICommandHandler<TestQuizQuestionsDeleteCommand, TestQuizQuestionsDeleteResponse>
{
    public async Task<TestQuizQuestionsDeleteResponse> Handle(TestQuizQuestionsDeleteCommand request, CancellationToken cancellationToken)
    {
        return await testService.DeleteTestQuizQuestionsAsync(request, cancellationToken);
    }
}

