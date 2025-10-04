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
}

public class IdentityEntity
{
    public Guid UserId { get; set; }
    
    public string Email { get; set; } = null!;
}