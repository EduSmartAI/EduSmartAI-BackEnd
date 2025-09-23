namespace QuizService.Domain.ReadModels;

public class PlacementTestQuizSettingCollection
{
    public Guid SubjectCode { get; set; }
    
    public string SubjectCodeName { get; set; }
    
    public string Title { get; set; } = null!;
    
    public string? Description { get; set; }
}