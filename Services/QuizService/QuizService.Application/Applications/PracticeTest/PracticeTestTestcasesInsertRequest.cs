using MediatR;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestTestcasesInsertRequest : IRequest<PracticeTestTestcasesInsertResponse>
{
    public Guid ProblemId { get; set; }
    
    public List<PracticeTestTestcaseAddRequestEntity>? PublicTestcases { get; set; }
    
    public List<PracticeTestTestcaseAddRequestEntity>? PrivateTestcases { get; set; }
}

public class PracticeTestTestcaseAddRequestEntity
{
    public string InputData { get; set; } = null!;

    public string ExpectedOutput { get; set; } = null!;
}

