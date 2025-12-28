using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.Admin.Queries.PracticeTests;

public record AdminPracticeTestSelectResponse : AbstractApiResponse<AdminPracticeTestSelectResponseEntity>
{
    public override AdminPracticeTestSelectResponseEntity Response { get; set; }
}

public class AdminPracticeTestSelectResponseEntity
{
    public Guid ProblemId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Difficulty { get; set; } = null!;
    
    public List<AdminPracticeTestExample> Examples { get; set; } = new();
    
    public List<AdminPracticeTestTestCase> TestCases { get; set; } = new();
    
    public List<AdminPracticeTestTemplate> Templates { get; set; } = new();
    
    public List<AdminPracticeSolution> Solutions { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
}

public class AdminPracticeSolution
{
    public Guid SolutionId { get; set; }

    public LanguageInfo Language { get; set; } = null!;

    public string SolutionCode { get; set; } = null!;
    
    public class LanguageInfo
    {
        public int LanguageId { get; set; }
        
        public string LanguageName { get; set; } = null!;
    }
}

public class AdminPracticeTestTestCase
{
    public Guid TestcaseId { get; set; }

    public string InputData { get; set; } = null!;

    public string ExpectedOutput { get; set; } = null!;
    
    public bool IsPublic { get; set; }
    
}

public class AdminPracticeTestExample
{
    public Guid ExampleId { get; set; }
    
    public int ExampleOrder { get; set; }

    public string InputData { get; set; } = null!;

    public string OutputData { get; set; } = null!;

    public string? Explanation { get; set; }
    
}

public class AdminPracticeTestTemplate
{
    public Guid TemplateId { get; set; }
    
    public int LanguageId { get; set; }
    
    public string LanguageName { get; set; } = null!;
    
    public string TemplatePrefix { get; set; } = null!;
    
    public string TemplateSuffix { get; set; } = null!;
    
    public string UserStubCode { get; set; } = null!;
}

