using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AiService.StudentInterestSurveyAnalysisEvents;

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

public record StudentInterestSurveyAnalysisEventResponse : AbstractApiResponse<StudentInterestAnalysisResult>
{
    public override StudentInterestAnalysisResult Response { get; set; }
}

public class StudentInterestAnalysisResult
{
    public string LearningGoal { get; set; } = null!;
}
