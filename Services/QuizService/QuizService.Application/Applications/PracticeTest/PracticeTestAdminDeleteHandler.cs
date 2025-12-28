using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestAdminDeleteHandler(IPracticeTestService practiceTestService) : IRequestHandler<PracticeTestAdminDeleteRequest, PracticeTestAdminDeleteResponse>
{
    public async Task<PracticeTestAdminDeleteResponse> Handle(PracticeTestAdminDeleteRequest request, CancellationToken cancellationToken)
    {
        return await practiceTestService.DeletePracticeTestAsync(request, cancellationToken);
    }
}

