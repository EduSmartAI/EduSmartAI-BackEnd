using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class StudentAnswer
{
    public Guid StudentAnswerId { get; set; }

    public Guid StudentTestId { get; set; }

    public Guid QuestionId { get; set; }

    public Guid? AnswerId { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual Answer? Answer { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual StudentTest StudentTest { get; set; } = null!;
}
