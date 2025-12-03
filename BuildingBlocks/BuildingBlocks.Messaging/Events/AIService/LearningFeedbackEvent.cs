namespace BuildingBlocks.Messaging.Events.AIService;

public class LearningFeedbackEvent
{
    public string SummaryFeedback { get; set; }

    public string HabitAndInterestAnalysis { get; set; }

    public string Personality { get; set; }

    public string LearningAbility { get; set; }
    
    public required string Email { get; set; } = null!;
    
    public required Guid LearningPathMajorId { get; set; }
    
    public required Guid LearningPathId { get; set; }
    
    public List<LearningPathSubjectCodeEvent> LearningPathSubjectCodes { get; set; } = null!;
}

public class LearningPathSubjectCodeEvent
{
    public string SubjectCode { get; set; } = null!;

    public string? AnalysisMarkdown { get; set; }
    
    public string Status { get; set; } = null!;
}
