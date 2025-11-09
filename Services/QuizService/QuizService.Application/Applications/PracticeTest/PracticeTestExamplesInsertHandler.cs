using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestExamplesInsertHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestExamplesInsertRequest, PracticeTestExamplesInsertResponse>
{
    public async Task<PracticeTestExamplesInsertResponse> Handle(PracticeTestExamplesInsertRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.InsertPracticeTestExamplesAsync(request, cancellationToken);
    }
}

