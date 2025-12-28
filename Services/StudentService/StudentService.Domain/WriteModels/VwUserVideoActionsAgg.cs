using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class VwUserVideoActionsAgg
{
    public Guid? StudentId { get; set; }

    public Guid? CourseId { get; set; }

    public Guid? TargetId { get; set; }

    public string? TargetType { get; set; }

    public string? ActionType { get; set; }

    public long? ActionCount { get; set; }
}
