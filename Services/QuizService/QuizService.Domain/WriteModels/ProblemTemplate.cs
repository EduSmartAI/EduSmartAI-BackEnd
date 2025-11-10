using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class ProblemTemplate
{
    public Guid TemplateId { get; set; }

    public Guid ProblemId { get; set; }

    public int LanguageId { get; set; }

    public string TemplatePrefix { get; set; } = null!;
    
    public string TemplateSuffix { get; set; } = null!;
    
    public string UserStubCode { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual CodeLanguage Language { get; set; } = null!;

    public virtual Problem Problem { get; set; } = null!;
}
