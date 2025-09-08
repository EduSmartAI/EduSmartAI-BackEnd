using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class TechnologyType
{
    public Guid TechnologyId { get; set; }

    public Guid TypeId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual Technology Technology { get; set; } = null!;

    public virtual Type Type { get; set; } = null!;
}
