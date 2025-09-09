using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class Major
{
    public Guid MajorId { get; set; }

    public string MajorName { get; set; } = null!;
    
    public string MajorCode { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
