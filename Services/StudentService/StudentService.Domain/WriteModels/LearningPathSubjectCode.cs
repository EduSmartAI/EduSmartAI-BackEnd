using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class LearningPathSubjectCode
{
    public Guid LearningPathSubjectCodeId { get; set; }

    public string SubjectCode { get; set; } = null!;

    public string? AnalysisMarkdown { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsActive { get; set; }

    public Guid LearningPathId { get; set; }

    public virtual LearningPath LearningPath { get; set; } = null!;

    public virtual ICollection<LearningPathCourse> LearningPathCourses { get; set; } = new List<LearningPathCourse>();
}
