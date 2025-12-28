using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class Quiz
{
    public Guid QuizId { get; set; }

    /// <summary>
    /// 1: Survey, 2: PlacementTest, 3: Course Quiz
    /// </summary>
    public short QuizType { get; set; }

    public Guid? TestId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual CourseQuizSetting? CourseQuizSetting { get; set; }

    public virtual PlacementTestQuizSetting? PlacementTestQuizSetting { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<StudentQuiz> StudentQuizzes { get; set; } = new List<StudentQuiz>();

    public virtual SurveyQuizSetting? SurveyQuizSetting { get; set; }

    public virtual Test? Test { get; set; }
}
