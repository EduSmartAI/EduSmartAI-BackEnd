using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class LearningPathMajorType
{
    public Guid TypeId { get; set; }

    public Guid LearningPathMajorId { get; set; }

    public string TypeName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual LearningPathMajor LearningPathMajor { get; set; } = null!;
}
