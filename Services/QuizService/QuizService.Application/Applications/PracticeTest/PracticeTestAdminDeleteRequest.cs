using MediatR;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestAdminDeleteRequest : IRequest<PracticeTestAdminDeleteResponse>
{
    public Guid ProblemId { get; set; }
}

