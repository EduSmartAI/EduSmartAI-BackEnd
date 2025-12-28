namespace QuizService.Domain.ReadModels;

public class SurveyQuizSettingCollection
{
    public short SurveyTypeId { get; set; }
    
    public string SurveyTypeName { get; set; } = null!;
    
    public string SurveyCode { get; set; } = null!;
    
    public string Title { get; set; } = null!;
    
    public string? Description { get; set; }
}