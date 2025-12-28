namespace BuildingBlocks.Messaging.Events.QuizService;

public class MajorAndSemesterSelectEvent
{
    public Guid? SemesterId { get; set; }
    
    public Guid? MajorId { get; set; }
}