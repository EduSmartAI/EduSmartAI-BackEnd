using BuildingBlocks.Messaging.Events.CourseService.QuizCourseCheckAttemptEvents;
using QuizService.Application.Applications.QuizCourses.Commands;
using QuizService.Application.Applications.QuizCourses.Queries;

namespace QuizService.Application.Interfaces;

public interface IQuizCourseService
{
    Task<QuizCourseInsertResponse> InsertQuizCourseAsync(QuizCourseInsertCommand request);
    
    Task<QuizCourseUpdateResponse> UpdateQuizCourseAsync(QuizCourseUpdateCommand request, CancellationToken cancellationToken);
    
    Task<QuizCourseAddQuestionsResponse> InsertQuestionsToQuizAsync(QuizCourseAddQuestionsCommand request, CancellationToken cancellationToken);
    
    Task<QuizCourseDeleteQuestionsResponse> DeleteQuestionsFromQuizAsync(QuizCourseDeleteQuestionsCommand request, CancellationToken cancellationToken);
    
    Task<QuizCourseSelectQueryResponse> SelectCourseQuiz(QuizCourseSelectQuery request);
    
    Task<StudentQuizCourseInsertResponse> InsertStudentQuizCourseAsync(StudentQuizCourseInsertCommand request, CancellationToken cancellationToken);
    
    Task<StudentCourseQuizSelectResponse> SelectStudentCourseQuizAsync(StudentCourseQuizSelectQuery request);
    Task<QuizCourseCheckAttemptResponse> CheckStudentQuizAttemptAsync(QuizCourseCheckAttemptCommand request, CancellationToken cancellationToken);
}