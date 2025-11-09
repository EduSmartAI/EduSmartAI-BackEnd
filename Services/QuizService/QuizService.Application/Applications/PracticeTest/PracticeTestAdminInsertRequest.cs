using MediatR;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestAdminInsertRequest : IRequest<PracticeTestAdminInsertResponse>
{
    public PracticeTestAdminProblemInsertRequest Problem { get; set; } = null!;
    
    public List<PracticeTestAdminProblemTestcaseInsertRequest> Testcases { get; set; } = null!;
    
    public List<PracticeTestAdminProblemTemplateInsertRequest> Templates { get; set; } = null!;
    
    public List<PracticeTestAdminProblemExampleInsertRequest> Examples { get; set; } = null!;
}

public class PracticeTestAdminProblemInsertRequest
{
    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Difficulty { get; set; } = null!;
}

public class PracticeTestAdminProblemTemplateInsertRequest
{
    public int LanguageId { get; set; }

    public string UserTemplatePrefix { get; set; } = null!;
    
    public string UserTemplateSuffix { get; set; } = null!;
    
    public string UserStubCode { get; set; } = null!;

}

public class PracticeTestAdminProblemExampleInsertRequest
{
    public int ExampleOrder { get; set; }

    public string InputData { get; set; } = null!;

    public string OutputData { get; set; } = null!;

    public string? Explanation { get; set; }
}

public class PracticeTestAdminProblemTestcaseInsertRequest
{
    public List<PracticeTestAdminProblemTestcasePublicInsertRequest> PublicTestcases { get; set; } = null!;
    
    public List<PracticeTestAdminProblemTestcasePrivateInsertRequest> PrivateTestcases { get; set; } = null!;
}

public class PracticeTestAdminProblemTestcasePublicInsertRequest
{
    public string InputData { get; set; } = null!;

    public string ExpectedOutput { get; set; } = null!;
}

public class PracticeTestAdminProblemTestcasePrivateInsertRequest : PracticeTestAdminProblemTestcasePublicInsertRequest
{
    
}