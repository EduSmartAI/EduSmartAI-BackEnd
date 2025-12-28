using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class Submission
{
    public Guid SubmissionId { get; set; }

    public Guid StudentId { get; set; }

    public Guid? ProblemId { get; set; }

    public string Code { get; set; } = null!;

    public int LanguageId { get; set; }

    public string Status { get; set; } = null!;

    public int? RuntimeMs { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual CodeLanguage Language { get; set; } = null!;

    public virtual Problem? Problem { get; set; }

    public virtual ICollection<SubmissionTestResult> SubmissionTestResults { get; set; } = new List<SubmissionTestResult>();
}
