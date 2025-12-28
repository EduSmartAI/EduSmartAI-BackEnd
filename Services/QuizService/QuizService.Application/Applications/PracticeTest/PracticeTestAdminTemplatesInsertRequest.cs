using MediatR;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestAdminTemplatesInsertRequest : IRequest<PracticeTestAdminTemplatesInsertResponse>
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

