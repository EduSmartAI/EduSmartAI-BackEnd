using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class StudentOrientation
{
    public Guid StudentOrientationId { get; set; }

    public Guid StudentId { get; set; }

    public string Technology { get; set; } = null!;

    public short RecommendType { get; set; }

    public string ReasonRecommend { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual Student Student { get; set; } = null!;
}
