using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService.CourseMajorSemesterSelectEvents;

public record CourseMajorSemesterSelectEventResponse : AbstractApiResponse<CourseMajorSemesterSelectEventResponseEntity>
{
    public override CourseMajorSemesterSelectEventResponseEntity Response { get; set; }
}

public class CourseMajorSemesterSelectEventResponseEntity
{
    public string SemesterName { get; set; }
    
    public short SemesterNumber { get; set; }
    
    public string MajorName { get; set; }
}