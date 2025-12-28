using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class LearningPathMajor
{
    public Guid LearningPathMajorId { get; set; }

    public Guid PathId { get; set; }

    public string MajorCode { get; set; } = null!;

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    /// <summary>
    /// 1: Internal, 2: External
    /// </summary>
    public short Type { get; set; }

    public int? PositionIndex { get; set; }

    public virtual ICollection<LearningPathCourse> LearningPathCourses { get; set; } = new List<LearningPathCourse>();

    public virtual ICollection<LearningPathSubjectCode> LearningPathSubjectCodes { get; set; } = new List<LearningPathSubjectCode>();

    public virtual LearningPath Path { get; set; } = null!;
}
