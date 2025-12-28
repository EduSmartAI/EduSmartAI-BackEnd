using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.QuizCourses.Queries;

public class QuizCourseSelectQuery : IQuery<QuizCourseSelectQueryResponse>
{
    public Guid QuizId { get; set; }
}