using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class StudentQuiz
{
    public Guid StudentQuizId { get; set; }

    public Guid StudentId { get; set; }

    public Guid QuizId { get; set; }

    public short QuizType { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual ICollection<StudentQuizAnswer> StudentQuizAnswers { get; set; } = new List<StudentQuizAnswer>();
}
