using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.QuizCourses.Queries;

public class StudentCourseQuizSelectQueryHandler(IQuizCourseService quizCourseService) : IQueryHandler<StudentCourseQuizSelectQuery, StudentCourseQuizSelectResponse>
{
    public async Task<StudentCourseQuizSelectResponse> Handle(StudentCourseQuizSelectQuery request, CancellationToken cancellationToken)
    {
        return await quizCourseService.SelectStudentCourseQuizAsync(request);
    }
}