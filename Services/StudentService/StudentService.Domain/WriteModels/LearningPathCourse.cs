using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class LearningPathCourse
{
    public Guid LearningPathCourseId { get; set; }

    public Guid LearningPathMajorId { get; set; }

    public Guid CourseId { get; set; }

    public int? Position { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual LearningPathMajor LearningPathMajor { get; set; } = null!;
}
