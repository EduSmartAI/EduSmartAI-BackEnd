using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestLanguageSelectsHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestLanguageSelectsRequest, PracticeTestLanguageSelectsResponse>
{
    public async Task<PracticeTestLanguageSelectsResponse> Handle(PracticeTestLanguageSelectsRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.SelectPracticeTestLanguagesAsync(request, cancellationToken);
    }
}