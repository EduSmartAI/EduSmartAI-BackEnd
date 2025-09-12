using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class Question
{
    public Guid QuestionId { get; set; }

    public Guid QuizId { get; set; }

    public string QuestionText { get; set; } = null!;
    
    public string? Explanation { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}
