using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestUserTemplateCodeSelectHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestUserTemplateCodeSelectRequest, PracticeTestUserTemplateCodeSelectResponse>
{
    public async Task<PracticeTestUserTemplateCodeSelectResponse> Handle(PracticeTestUserTemplateCodeSelectRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.SelectUserStubCodeAsync(request, cancellationToken);
    }
}