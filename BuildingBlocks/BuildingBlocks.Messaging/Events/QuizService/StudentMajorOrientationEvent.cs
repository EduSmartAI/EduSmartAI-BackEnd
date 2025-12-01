namespace BuildingBlocks.Messaging.Events.QuizService;

public class StudentMajorOrientationEvent
{
    public string LearningGoal { get; set; } = null!;

    public List<string> Languages { get; set; } = null!;

    public List<string> Frameworks { get; set; } = null!;

    public string LimitTime { get; set; } = null!;

    public IdentityEntity IdentityEntity { get; set; } = null!;
    
    public Guid LearningPathId { get; set; }
    
    public Guid SemesterId { get; set; }
    
    public short StudentLevel { get; set; }
    
    public List<string>? StudentPassedSubjects { get; set; }
    public required List<CourseImprove>? CourseImproves { get; set; }
    
    public List<StudentSubjectMark>? SubjectMarks { get; set; }
    public List<StudentAbilityMark>? AbilityMarks { get; set; }
    public StudentQuizSurvey? QuizSurvey { get; set; }
}

/// <summary>
/// Subject mark information for AI analysis
/// </summary>
public class StudentSubjectMark
{
    public string SubjectCode { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public double? Mark { get; set; }
}

/// <summary>
/// Ability mark information for AI analysis (from placement test)
/// </summary>
public class StudentAbilityMark
{
    public string Name { get; set; } = null!;
    public double Mark { get; set; }
}

/// <summary>
/// Quiz survey responses for AI analysis
/// </summary>
public class StudentQuizSurvey
{
    public List<StudentQuizInterest> QuizInterests { get; set; } = new();
    public List<StudentQuizHabit> QuizHabits { get; set; } = new();
}

public class StudentQuizInterest
{
    public string Question { get; set; } = null!;
    public string Answer { get; set; } = null!;
}

public class StudentQuizHabit
{
    public string Question { get; set; } = null!;
    public string Answer { get; set; } = null!;
}

public class IdentityEntity
{
    public Guid UserId { get; set; }
    
    public string Email { get; set; } = null!;
}