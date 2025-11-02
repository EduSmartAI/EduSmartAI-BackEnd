using System;
using System.Collections.Generic;

namespace TeacherService.Domain.WriteModels;

public partial class TeacherRating
{
    public Guid RatingId { get; set; }

    public Guid TeacherId { get; set; }

    public Guid? StudentId { get; set; }

    public short Rating { get; set; }

    public string? Review { get; set; }

    public string? ReviewTitle { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsActive { get; set; }

    public virtual Teacher Teacher { get; set; } = null!;
}
