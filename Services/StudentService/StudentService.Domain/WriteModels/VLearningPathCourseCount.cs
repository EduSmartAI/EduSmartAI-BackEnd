using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class VLearningPathCourseCount
{
    public Guid? PathId { get; set; }

    public string? PathName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? StudentId { get; set; }

    public short? Status { get; set; }

    public long? TotalCourses { get; set; }
}
