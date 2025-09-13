using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class StudentSurveyAnswer
{
    public Guid StudentSurveyAnswerId { get; set; }

    public Guid StudentSurveyId { get; set; }

    public Guid QuestionId { get; set; }

    public Guid? AnswerId { get; set; }

    public string? AnswerText { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual Answer? Answer { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual StudentSurvey StudentSurvey { get; set; } = null!;
}
