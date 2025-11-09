using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestTemplatesInsertHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestTemplatesInsertRequest, PracticeTestTemplatesResponse>
{
    public async Task<PracticeTestTemplatesResponse> Handle(PracticeTestTemplatesInsertRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.InsertPracticeTestTemplatesAsync(request, cancellationToken);
    }
}

