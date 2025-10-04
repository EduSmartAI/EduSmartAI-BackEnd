using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class Technology
{
    public Guid TechnologyId { get; set; }

    public string TechnologyName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public short TechnologyType { get; set; }

    public virtual ICollection<StudentTechnology> StudentTechnologies { get; set; } = new List<StudentTechnology>();
}
