using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class StudentSurvey
{
    public Guid StudentSurveyId { get; set; }

    public Guid StudentId { get; set; }

    public Guid SurveyId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<StudentSurveyAnswer> StudentSurveyAnswers { get; set; } = new List<StudentSurveyAnswer>();

    public virtual Quiz Survey { get; set; } = null!;
}
