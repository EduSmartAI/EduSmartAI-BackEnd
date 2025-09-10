using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Tests.Queries;

public class TestSelectQueryHandler : IQueryHandler<TestSelectQuery, TestSelectResponse>
{
    private readonly ITestService _testService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="testService"></param>
    public TestSelectQueryHandler(ITestService testService)
    {
        _testService = testService;
    }

    /// <summary>
    /// Handle select test
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TestSelectResponse> Handle(TestSelectQuery request, CancellationToken cancellationToken)
    {
        return await _testService.SelectTestAsync(request);
    }
}