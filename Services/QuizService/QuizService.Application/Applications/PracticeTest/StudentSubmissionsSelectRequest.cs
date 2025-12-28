using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.PracticeTest;

public record StudentSubmissionsSelectRequest : IQuery<StudentSubmissionsSelectResponse>
{
    public Guid? ProblemId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

