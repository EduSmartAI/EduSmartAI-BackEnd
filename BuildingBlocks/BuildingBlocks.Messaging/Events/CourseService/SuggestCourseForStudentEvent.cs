namespace BuildingBlocks.Messaging.Events.CourseService;

public class SuggestCourseForStudentEvent
{
    public List<SuggestCourseForStudentEventEntity> SuggestCourses { get; set; }
}

public class SuggestCourseForStudentEventEntity
{
    public Guid StudentId { get; set; }
    
    public string Email { get; set; } = null!;

    public Guid OriginalCourseId { get; set; }

    public Guid SuggestedCourseId { get; set; }

    public string Reason { get; set; } = null!;
}