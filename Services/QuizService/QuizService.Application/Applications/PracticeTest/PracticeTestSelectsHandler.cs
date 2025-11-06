using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestSelectsHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestSelectsRequest, PracticeTestSelectsResponse>
{
    public async Task<PracticeTestSelectsResponse> Handle(PracticeTestSelectsRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.SelectPracticeTestsAsync(request, cancellationToken);
    }
}