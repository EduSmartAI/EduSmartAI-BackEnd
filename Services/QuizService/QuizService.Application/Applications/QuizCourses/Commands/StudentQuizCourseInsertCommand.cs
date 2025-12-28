using BuildingBlocks.CQRS;
using QuizService.Application.Applications.StudentSurveys.Commands;

namespace QuizService.Application.Applications.QuizCourses.Commands;

public class StudentQuizCourseInsertCommand : ICommand<StudentQuizCourseInsertResponse>
{
    public Guid QuizId { get; set; }
    public Guid CourseId { get; set; }
	public Guid? ModuleId { get; set; }
    public Guid? LessonId { get; set; }
	public List<StudentQuizAnswerInsertRequest> StudentQuizAnswers { get; set; }
}