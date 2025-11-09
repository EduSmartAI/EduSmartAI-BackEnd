using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestAdminTestcasesInsertHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestAdminTestcasesInsertRequest, PracticeTestAdminTestcasesInsertResponse>
{
    public async Task<PracticeTestAdminTestcasesInsertResponse> Handle(PracticeTestAdminTestcasesInsertRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.InsertPracticeTestTestcasesAsync(request, cancellationToken);
    }
}

