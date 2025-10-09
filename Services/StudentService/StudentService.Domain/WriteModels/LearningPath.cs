using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class LearningPath
{
    public Guid PathId { get; set; }

    public string PathName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public Guid? StudentId { get; set; }

    public short Status { get; set; }

    public virtual ICollection<LearningPathMajor> LearningPathMajors { get; set; } = new List<LearningPathMajor>();

    public virtual Student? Student { get; set; }
}
