using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.CourseMajorSemesterSelectEvents;

public record CourseMajorSemesterSelectEventResponse : AbstractApiResponse<CourseMajorSemesterSelectEventResponseEntity>
{
    public override CourseMajorSemesterSelectEventResponseEntity Response { get; set; }
}

public class CourseMajorSemesterSelectEventResponseEntity
{
    public string SemesterName { get; set; }
    
    public string MajorName { get; set; }
}