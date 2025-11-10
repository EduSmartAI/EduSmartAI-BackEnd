using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestAdminTemplatesInsertHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestAdminTemplatesInsertRequest, PracticeTestAdminTemplatesInsertResponse>
{
    public async Task<PracticeTestAdminTemplatesInsertResponse> Handle(PracticeTestAdminTemplatesInsertRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.InsertPracticeTestTemplatesAsync(request, cancellationToken);
    }
}

