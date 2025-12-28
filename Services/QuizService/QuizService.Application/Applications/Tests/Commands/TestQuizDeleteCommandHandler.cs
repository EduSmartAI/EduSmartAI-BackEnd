using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Tests.Commands;

public class TestQuizDeleteCommandHandler(ITestService testService) : ICommandHandler<TestQuizDeleteCommand, TestQuizDeleteResponse>
{
    public async Task<TestQuizDeleteResponse> Handle(TestQuizDeleteCommand request, CancellationToken cancellationToken)
    {
        return await testService.DeleteTestQuizAsync(request, cancellationToken);
    }
}

