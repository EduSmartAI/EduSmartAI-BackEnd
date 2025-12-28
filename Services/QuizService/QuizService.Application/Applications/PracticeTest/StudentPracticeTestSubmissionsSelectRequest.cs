using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.PracticeTest;

public record StudentPracticeTestSubmissionsSelectRequest : IQuery<StudentPracticeTestSubmissionsSelectResponse>
{
    public Guid? SubmissionId { get; set; }
    public Guid? ProblemId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

