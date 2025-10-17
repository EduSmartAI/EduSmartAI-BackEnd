using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.QuizCourses.Queries;

public class QuizCourseSelectQueryHandler(IQuizCourseService quizCourseService) : IQueryHandler<QuizCourseSelectQuery, QuizCourseSelectQueryResponse>
{
    public async Task<QuizCourseSelectQueryResponse> Handle(QuizCourseSelectQuery request, CancellationToken cancellationToken)
    {
        return await quizCourseService.SelectCourseQuiz(request);
    }
}