using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class Quiz
{
    public Guid QuizId { get; set; }

    public Guid TestId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public Guid SubjectCode { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual Test Test { get; set; } = null!;
}
