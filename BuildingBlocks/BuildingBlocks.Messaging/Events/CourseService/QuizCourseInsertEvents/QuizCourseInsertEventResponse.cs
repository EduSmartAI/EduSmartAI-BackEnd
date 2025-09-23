using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.CourseService.QuizCourseInsertEvents;

public record QuizCourseInsertEventResponse : AbstractApiResponse<QuizCourseInsertEventResponseEntity>
{
    public override QuizCourseInsertEventResponseEntity Response { get; set; }
}

public class QuizCourseInsertEventResponseEntity
{
    public Guid QuizId { get; set; }
}