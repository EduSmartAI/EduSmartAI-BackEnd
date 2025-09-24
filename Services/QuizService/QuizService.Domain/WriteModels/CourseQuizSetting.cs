using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class CourseQuizSetting
{
    public Guid QuizId { get; set; }

    public int DurationMinutes { get; set; }

    public int PassingScorePercentage { get; set; }

    public bool? ShuffleQuestions { get; set; }

    public bool? ShowResultsImmediately { get; set; }

    public bool? AllowRetake { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual Quiz Quiz { get; set; } = null!;
}
