using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestSelectHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestSelectRequest, PracticeTestSelectResponse>
{
    public async Task<PracticeTestSelectResponse> Handle(PracticeTestSelectRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.SelectPracticeTestAsync(request, cancellationToken);
    }
}