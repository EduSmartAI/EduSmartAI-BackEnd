namespace BuildingBlocks.Messaging.Events.QuizService;

public class AiRecommendImprovementEvent
{
    public string CareerGoal { get; set; } = null!;
    public List<SubjectMarkEvent> SubjectMarks { get; set; }
    public List<AbilityMarkEvent>? AbilityMarks { get; set; }
    
    public List<MajorInfoEvent> Majors { get; set; } = new();
    
    public QuizSurveyEvent QuizSurveyEvent { get; set; }
    public required Guid LearningPathMajorId { get; set; }
    
    public required string Email { get; set; }
    public required Guid LearningPathId { get; set; }
    public List<StudentCurriculumEvent> StudentCurriculums { get; set; }
}

/// <summary>
/// Information about a major for AI processing
/// </summary>
public class MajorInfoEvent
{
    public string MajorCode { get; set; } = null!;
    public string MajorName { get; set; } = null!;
    
    public Guid? LearningPathMajorId { get; set; }
}
public class SubjectMarkEvent
{
    public string SubjectCode { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public double? Mark { get; set; }
}
public class AbilityMarkEvent
{
    public string Name { get; set; } = string.Empty;
    public double Mark { get; set; }
}

public class QuizSurveyEvent
{
    public List<QuizInterestEvent> QuizInterests { get; set; } = [];
    public List<QuizHabitEvent> QuizHabits { get; set; } = [];
}
public class QuizInterestEvent
{
    public string Question { get; set; } = null!;
    public string Answer { get; set; } = null!;
}
public class QuizHabitEvent
{
    public string Question { get; set; } = null!;
    public string Answer { get; set; } = null!;
}

public class AiRecommendImprovementEventResposne
{
    
}