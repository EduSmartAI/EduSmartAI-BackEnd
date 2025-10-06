using QuizService.Application.Applications.QuizCourses.Commands;
using QuizService.Application.Applications.QuizCourses.Queries;

namespace QuizService.Application.Interfaces;

public interface IQuizCourseService
{
    Task<QuizCourseInsertResponse> InsertQuizCourseAsync(QuizCourseInsertCommand request);
    
    Task<QuizCourseSelectQueryResponse> SelectCourseQuiz(QuizCourseSelectQuery request);
    
    Task<StudentQuizCourseInsertResponse> InsertStudentQuizCourseAsync(StudentQuizCourseInsertCommand request, CancellationToken cancellationToken);
    
    Task<StudentCourseQuizSelectResponse> SelectStudentCourseQuizAsync(StudentCourseQuizSelectQuery request);
}