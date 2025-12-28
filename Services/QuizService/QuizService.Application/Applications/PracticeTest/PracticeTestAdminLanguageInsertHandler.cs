using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestAdminLanguageInsertHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestAdminLanguageInsertRequest, PracticeTestAdminLanguageInsertResponse>
{
    public async Task<PracticeTestAdminLanguageInsertResponse> Handle(PracticeTestAdminLanguageInsertRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.InsertPracticeLanguageAsync(request, cancellationToken);
    }
}