using MediatR;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestAdminUpdateRequest : IRequest<PracticeTestAdminUpdateResponse>
{
    public Guid ProblemId { get; set; }
    
    public PracticeTestAdminProblemUpdateRequest Problem { get; set; } = null!;
    
    public List<PracticeTestAdminProblemTestcaseUpdateRequest>? Testcases { get; set; }
    
    public List<PracticeTestAdminProblemTemplateUpdateRequest>? Templates { get; set; }
    
    public List<PracticeTestAdminProblemExampleUpdateRequest>? Examples { get; set; }
}

public class PracticeTestAdminProblemUpdateRequest
{
    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Difficulty { get; set; }
}

public class PracticeTestAdminProblemTemplateUpdateRequest
{
    public Guid TemplateId { get; set; }
    
    public int LanguageId { get; set; }

    public string UserTemplatePrefix { get; set; } = null!;
    
    public string UserTemplateSuffix { get; set; } = null!;
    
    public string UserStubCode { get; set; } = null!;

}

public class PracticeTestAdminProblemExampleUpdateRequest
{
    public Guid ExampleId { get; set; }
    
    public int ExampleOrder { get; set; }

    public string InputData { get; set; } = null!;

    public string OutputData { get; set; } = null!;

    public string? Explanation { get; set; }
}

public class PracticeTestAdminProblemTestcaseUpdateRequest
{
    public Guid TestcaseId { get; set; }
    
    public string InputData { get; set; } = null!;

    public string ExpectedOutput { get; set; } = null!;
    
    public bool IsPublic { get; set; }
}

