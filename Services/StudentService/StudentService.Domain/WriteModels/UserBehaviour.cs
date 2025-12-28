using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class UserBehaviour
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public string ActionType { get; set; } = null!;

    public Guid? TargetId { get; set; }

    public string? TargetType { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public Guid? ParentTargetId { get; set; }

    public virtual Student Student { get; set; } = null!;
}
