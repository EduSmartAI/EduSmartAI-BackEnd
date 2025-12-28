using MediatR;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestAdminExamplesInsertRequest : IRequest<PracticeTestAdminExamplesInsertResponse>
{
    public Guid ProblemId { get; set; }
    
    public List<PracticeTestExampleAddRequestEntity> Examples { get; set; } = null!;
}

public class PracticeTestExampleAddRequestEntity
{
    public int ExampleOrder { get; set; }

    public string InputData { get; set; } = null!;

    public string OutputData { get; set; } = null!;

    public string? Explanation { get; set; }
}

