namespace BuildingBlocks.Messaging.Events.QuizService.StudentTechnologyOrientationEvents;

public class StudentMajorOrientationEvent
{
    public string LearningGoal { get; set; }
    
    public List<string> Languages { get; set; }
    
    public List<string> Frameworks { get; set; }
}