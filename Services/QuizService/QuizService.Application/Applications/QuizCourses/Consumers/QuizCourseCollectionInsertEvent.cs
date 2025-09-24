using QuizService.Domain.ReadModels;

namespace QuizService.Application.Applications.QuizCourses.Consumers;

public class QuizCourseCollectionInsertEvent
{
    public QuizCollection Quiz { get; set; } = null!;
}