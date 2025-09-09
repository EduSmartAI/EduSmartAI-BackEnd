using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class Type
{
    public Guid TypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<TechnologyType> TechnologyTypes { get; set; } = new List<TechnologyType>();
}
