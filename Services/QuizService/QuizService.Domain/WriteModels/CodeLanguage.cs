using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class CodeLanguage
{
    public int LanguageId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsArchived { get; set; }

    public string SourceFile { get; set; } = null!;

    public string CompileCmd { get; set; } = null!;

    public string RunCmd { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<ProblemSolution> ProblemSolutions { get; set; } = new List<ProblemSolution>();

    public virtual ICollection<ProblemTemplate> ProblemTemplates { get; set; } = new List<ProblemTemplate>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
