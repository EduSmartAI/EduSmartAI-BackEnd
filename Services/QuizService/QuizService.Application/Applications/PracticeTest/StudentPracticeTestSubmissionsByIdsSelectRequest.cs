using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.PracticeTest;

public record StudentPracticeTestSubmissionsByIdsSelectRequest : IQuery<StudentPracticeTestSubmissionsByIdsSelectResponse>
{
    public List<Guid> SubmissionIds { get; set; }
}

