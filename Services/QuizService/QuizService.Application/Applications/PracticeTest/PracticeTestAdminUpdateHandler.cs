using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestAdminUpdateHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestAdminUpdateRequest, PracticeTestAdminUpdateResponse>
{
    public async Task<PracticeTestAdminUpdateResponse> Handle(PracticeTestAdminUpdateRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.UpdatePracticeTestAsync(request, cancellationToken);
    }
}

