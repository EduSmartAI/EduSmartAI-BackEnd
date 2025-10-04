namespace BuildingBlocks.Messaging.Events.QuizService;

public class StudentMajorSemesterInformationEvent
{
    public Guid StudentId { get; set; }
    
    public Guid SemesterId { get; set; }
    
    public string SemesterName { get; set; }
    
    public Guid MajorId { get; set; }
    
    public string MajorName { get; set; }
    
    public List<Guid> ProgramingLanguages { get; set; }
    
    public Guid LearningGoalId { get; set; }
}