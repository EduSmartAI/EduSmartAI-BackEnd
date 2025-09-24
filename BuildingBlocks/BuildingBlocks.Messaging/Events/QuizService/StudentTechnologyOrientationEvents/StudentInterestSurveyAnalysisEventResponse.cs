using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService.StudentTechnologyOrientationEvents;

public record StudentInterestSurveyAnalysisEventResponse : AbstractApiResponse<StudentInterestAnalysisResult>
{
    public override StudentInterestAnalysisResult Response { get; set; }
}

public class StudentInterestAnalysisResult
{
    public List<string> SuggestedMajors { get; set; } = new();
    
    public List<string> SuggestedTechnologies { get; set; } = new();
    
    public string AnalysisReason { get; set; } = string.Empty;
}
