using QuizService.Domain.ReadModels;

namespace QuizService.Application.Applications.QuizCourses.Consumers;

public class StudentQuizCourseInsertEvent
{
    public StudentQuizCollection StudentQuiz { get; set; }
}