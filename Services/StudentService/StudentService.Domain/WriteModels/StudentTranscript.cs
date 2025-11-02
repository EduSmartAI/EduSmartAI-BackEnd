using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class StudentTranscript
{
    public Guid StudentTranscriptId { get; set; }

    public Guid StudentId { get; set; }

    public Guid SemesterId { get; set; }

    public string Semester { get; set; } = null!;

    public int SemesterNumber { get; set; }

    public string SubjectCode { get; set; } = null!;

    public string? Prerequisite { get; set; }

    public string SubjectName { get; set; } = null!;

    public int Credit { get; set; }

    public double Grade { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual Student Student { get; set; } = null!;
}
