using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.StudentTests.Commands;

public record StudentTestInsertResponse : AbstractApiResponse<Guid?>
{
    public override Guid? Response { get; set; }
    
    public StudentTestSubmitResponse StudentTestSubmit { get; set; } = null!;
}

public class StudentTestSubmitResponse
{
    public Guid StudentTestId { get; set; }
    
    public List<StudentPracticeTestSubmitResponse>? PracticeTestSubmits { get; set; }
}

public class StudentPracticeTestSubmitResponse
{
    public Guid PracticeTestSubmitId { get; set; }
}