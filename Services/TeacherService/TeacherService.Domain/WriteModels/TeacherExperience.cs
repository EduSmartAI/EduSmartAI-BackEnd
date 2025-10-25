using System;
using System.Collections.Generic;

namespace TeacherService.Domain.WriteModels;

public partial class TeacherExperience
{
    public Guid ExperienceId { get; set; }

    public Guid TeacherId { get; set; }

    public string? RoleTitle { get; set; }

    public string? Organization { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsActive { get; set; }

    public virtual Teacher Teacher { get; set; } = null!;
}
