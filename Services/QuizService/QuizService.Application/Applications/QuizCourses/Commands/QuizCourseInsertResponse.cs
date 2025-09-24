using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.QuizCourses.Commands;

public record QuizCourseInsertResponse : AbstractApiResponse<QuizCourseInsertResponseEntity>
{
    public override QuizCourseInsertResponseEntity Response { get; set; }
}

public class QuizCourseInsertResponseEntity
{
    public Guid QuizId { get; set; }
}