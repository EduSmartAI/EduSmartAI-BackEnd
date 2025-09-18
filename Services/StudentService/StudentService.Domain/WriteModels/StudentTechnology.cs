using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class StudentTechnology
{
    public Guid StudentId { get; set; }

    public Guid TechnologyId { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }
    
    public bool IsActive { get; set; }

    public virtual Student Student { get; set; } = null!;

    public virtual Technology Technology { get; set; } = null!;
}
