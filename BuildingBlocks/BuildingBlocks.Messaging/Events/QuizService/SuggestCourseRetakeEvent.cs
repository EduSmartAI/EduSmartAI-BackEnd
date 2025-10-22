using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService;

public record SuggestCourseRetakeEvent
{
    public Guid CourseId { get; init; }
}

public record SuggestCourseRetakeEventResponse : AbstractApiResponse<List<SuggestCourseRetakeEventResponseEntity>>
{
    public override List<SuggestCourseRetakeEventResponseEntity> Response { get; set; }
}

public class SuggestCourseRetakeEventResponseEntity
{
    public Guid CourseId { get; set; }
    
    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int? DurationMinutes { get; set; }

    public short? Level { get; set; }
    
    public string CourseImageUrl { get; set; } = null!;
}
