using System;
using System.Collections.Generic;

namespace StudentService.Domain.Model;

public partial class Technologytype
{
    public Guid TechnologytypeId { get; set; }

    public Guid TechnologyId { get; set; }

    public Guid TypeId { get; set; }

    public virtual Technology Technology { get; set; } = null!;

    public virtual Type Type { get; set; } = null!;
}
