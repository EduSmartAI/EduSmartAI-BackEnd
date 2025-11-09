using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestTestcasesInsertHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestTestcasesInsertRequest, PracticeTestTestcasesInsertResponse>
{
    public async Task<PracticeTestTestcasesInsertResponse> Handle(PracticeTestTestcasesInsertRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.InsertPracticeTestTestcasesAsync(request, cancellationToken);
    }
}

