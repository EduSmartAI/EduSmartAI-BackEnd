using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Tests.Commands;

public class TestInsertCommandHandler : ICommandHandler<TestInsertCommand, TestInsertResponse>
{
    private readonly ITestService _testService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="testService"></param>
    public TestInsertCommandHandler(ITestService testService)
    {
        _testService = testService;
    }

    /// <summary>
    /// Handle insert test
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TestInsertResponse> Handle(TestInsertCommand request, CancellationToken cancellationToken)
    {
        return await _testService.InsertTestAsync(request, cancellationToken);
    }
}