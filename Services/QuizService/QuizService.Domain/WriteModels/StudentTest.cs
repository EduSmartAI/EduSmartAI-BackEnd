using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class StudentTest
{
    public Guid StudentTestId { get; set; }

    public Guid StudentId { get; set; }

    public Guid TestId { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();

    public virtual Test Test { get; set; } = null!;
}
