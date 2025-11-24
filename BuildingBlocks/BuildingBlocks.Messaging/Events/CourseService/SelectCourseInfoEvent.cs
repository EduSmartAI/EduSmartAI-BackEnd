using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.CourseService;

public record SelectCourseInfoEvent
{
    public Guid CourseId { get; init; }
}

public record SelectCourseInfoEventResponse : AbstractApiResponse<SelectCourseInfoEventResponseEntity>
{
    public override SelectCourseInfoEventResponseEntity Response { get; set; }
}

public class SelectCourseInfoEventResponseEntity
{
    public Guid CourseId { get; init; }
    
    public string SubjectCode { get; init; } = null!;
    
    public short Level { get; init; }
    
    public string Title { get; init; } = null!;
    
    public string ImageUrl { get; init; } = null!;
    
    public decimal DealPrice { get; init; }
    
    public decimal Price { get; init; }
}

