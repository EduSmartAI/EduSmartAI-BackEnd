using QuizService.Domain.ReadModels;

namespace QuizService.Application.Applications.QuizCourses.Consumers;

public class QuizCourseCollectionUpsertEvent
{
    public QuizCollection Quiz { get; set; } = null!;
}