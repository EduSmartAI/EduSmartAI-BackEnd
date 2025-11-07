using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestSubmitInsertHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestSubmitInsertRequest, PracticeTestSubmitInsertResponse>
{
    public async Task<PracticeTestSubmitInsertResponse> Handle(PracticeTestSubmitInsertRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.InsertPracticeTestSubmitAsync(request, cancellationToken);
    }
}