using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class Problem
{
    public Guid ProblemId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Difficulty { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<ProblemExample> ProblemExamples { get; set; } = new List<ProblemExample>();

    public virtual ICollection<ProblemTemplate> ProblemTemplates { get; set; } = new List<ProblemTemplate>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
}
