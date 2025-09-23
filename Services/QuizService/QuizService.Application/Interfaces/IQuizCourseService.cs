using QuizService.Application.Applications.QuizCourses.Commands;
using QuizService.Application.Applications.QuizCourses.Queries;

namespace QuizService.Application.Interfaces;

public interface IQuizCourseService
{
    Task<QuizCourseInsertResponse> InsertQuizCourseAsync(QuizCourseInsertCommand request);
    
    Task<QuizCourseSelectQueryResponse> SelectCourseQuiz(QuizCourseSelectQuery request);
}