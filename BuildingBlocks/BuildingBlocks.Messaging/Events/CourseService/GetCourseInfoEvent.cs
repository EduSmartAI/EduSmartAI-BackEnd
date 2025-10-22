namespace BuildingBlocks.Messaging.Events.CourseService;

public record GetCourseInfoEvent
{
    public Guid CourseId { get; init; }
}

public record GetCourseInfoEventResponse
{
    public bool Success { get; init; }
    public Guid CourseId { get; init; }
    public string? SubjectCode { get; init; }
    public short Level { get; init; }
    public string? Title { get; init; }
}

