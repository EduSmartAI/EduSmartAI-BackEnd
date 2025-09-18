namespace BuildingBlocks.Messaging.Events.CourseMajorSemesterSelectEvents;

public class CourseMajorSemesterSelectEvent
{
    public Guid SemesterId { get; set; }
    
    public Guid MajorId { get; set; }
}