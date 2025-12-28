using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestAdminExamplesInsertHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestAdminExamplesInsertRequest, PracticeTestAdminExamplesInsertResponse>
{
    public async Task<PracticeTestAdminExamplesInsertResponse> Handle(PracticeTestAdminExamplesInsertRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.InsertPracticeTestExamplesAsync(request, cancellationToken);
    }
}

