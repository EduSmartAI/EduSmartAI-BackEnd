using MediatR;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestSelectsRequest : IRequest<PracticeTestSelectsResponse>
{
    public int PageNumber { get; init; } = 1;
    
    public int PageSize { get; init; } = 10;
    
    public string SearchTerm { get; init; }
}