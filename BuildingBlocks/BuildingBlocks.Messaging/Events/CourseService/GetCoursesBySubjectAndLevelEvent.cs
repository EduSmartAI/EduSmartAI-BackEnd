namespace BuildingBlocks.Messaging.Events.CourseService;

public record GetCoursesBySubjectAndLevelEvent
{
    public string SubjectCode { get; init; } = null!;
    public short Level { get; init; }
    public Guid? ExcludeCourseId { get; init; }
}

public record CourseBasicDto
{
    public Guid CourseId { get; init; }
    public string Title { get; init; } = null!;
    public short Level { get; init; }
    public string SubjectCode { get; init; } = null!;
    public string? ShortDescription { get; init; }
}

public record GetCoursesBySubjectAndLevelEventResponse
{
    public bool Success { get; init; }
    public List<CourseBasicDto> Courses { get; init; } = new();
}

