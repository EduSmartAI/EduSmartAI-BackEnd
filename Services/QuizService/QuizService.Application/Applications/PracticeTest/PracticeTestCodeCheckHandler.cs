using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestCodeCheckHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestCodeCheckRequest, PracticeTestCodeCheckResponse>
{
    public async Task<PracticeTestCodeCheckResponse> Handle(PracticeTestCodeCheckRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.CheckPracticeTestCodeAsync(request, cancellationToken);
    }
}

