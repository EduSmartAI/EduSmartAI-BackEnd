using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestAdminInsertHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestAdminInsertRequest, PracticeTestAdminInsertResponse>
{
    public async Task<PracticeTestAdminInsertResponse> Handle(PracticeTestAdminInsertRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.InsertPracticeTestAsync(request, cancellationToken);
    }
}

