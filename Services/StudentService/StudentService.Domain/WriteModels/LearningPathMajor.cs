using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class LearningPathMajor
{
    public Guid LearningPathMajorId { get; set; }

    public Guid PathId { get; set; }

    public Guid MajorId { get; set; }

    public string? Reason { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<LearningPathCourse> LearningPathCourses { get; set; } = new List<LearningPathCourse>();

    public virtual ICollection<LearningPathMajorType> LearningPathMajorTypes { get; set; } = new List<LearningPathMajorType>();

    public virtual LearningPath Path { get; set; } = null!;
}
