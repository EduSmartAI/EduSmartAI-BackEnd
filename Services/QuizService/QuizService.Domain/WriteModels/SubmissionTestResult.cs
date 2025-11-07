using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class SubmissionTestResult
{
    public Guid SubmissionTestId { get; set; }

    public Guid? SubmissionId { get; set; }

    public Guid? TestcaseId { get; set; }

    public bool? Passed { get; set; }

    public string? ActualOutput { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual Submission? Submission { get; set; }

    public virtual TestCase? Testcase { get; set; }
}
