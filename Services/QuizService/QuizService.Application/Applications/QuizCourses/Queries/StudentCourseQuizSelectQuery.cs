using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.QuizCourses.Queries;

public class StudentCourseQuizSelectQuery : IQuery<StudentCourseQuizSelectResponse>
{
    public Guid StudentQuizCourseId { get; set; }
}