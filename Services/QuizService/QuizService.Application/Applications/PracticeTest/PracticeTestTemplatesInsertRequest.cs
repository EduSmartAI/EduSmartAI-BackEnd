using MediatR;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestTemplatesInsertRequest : IRequest<PracticeTestTemplatesResponse>
{
    public Guid ProblemId { get; set; }
    
    public List<PracticeTestTemplateAddRequestEntity> Templates { get; set; } = null!;
}

public class PracticeTestTemplateAddRequestEntity
{
    public int LanguageId { get; set; }

    public string UserTemplatePrefix { get; set; } = null!;
    
    public string UserTemplateSuffix { get; set; } = null!;
    
    public string UserStubCode { get; set; } = null!;
}

