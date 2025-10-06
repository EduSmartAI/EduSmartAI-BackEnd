using BuildingBlocks.CQRS;
using QuizService.Application.Applications.StudentSurveys.Commands;
using QuizService.Domain.WriteModels;

namespace QuizService.Application.Applications.QuizCourses.Commands;

public class StudentQuizCourseInsertCommand : ICommand<StudentQuizCourseInsertResponse>
{
    public Guid QuizId { get; set; }
    public List<StudentQuizAnswerInsertRequest> StudentQuizAnswers { get; set; }
}