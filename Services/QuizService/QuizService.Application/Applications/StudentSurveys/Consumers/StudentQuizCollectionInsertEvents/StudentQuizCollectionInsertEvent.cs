using QuizService.Domain.ReadModels;

namespace QuizService.Application.Applications.StudentSurveys.Consumers.StudentQuizCollectionInsertEvents;

public class StudentQuizCollectionInsertEvent
{
    public List<StudentQuizCollection> StudentQuizzes { get; set; }
}