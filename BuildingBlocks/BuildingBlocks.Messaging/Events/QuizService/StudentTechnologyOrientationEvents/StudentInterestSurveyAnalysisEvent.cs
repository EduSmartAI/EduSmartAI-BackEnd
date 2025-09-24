namespace BuildingBlocks.Messaging.Events.QuizService.StudentTechnologyOrientationEvents;

public class StudentInterestSurveyAnalysisEvent
{
    public Guid StudentId { get; set; }
    
    public List<StudentInterestQuestion> Questions { get; set; } = new();
}

public class StudentInterestQuestion
{
    public Guid QuestionId { get; set; }
    
    public string QuestionText { get; set; } = string.Empty;
    
    public List<string> StudentAnswers { get; set; } = new();
}
