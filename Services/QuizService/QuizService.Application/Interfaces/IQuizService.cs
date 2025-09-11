namespace QuizService.Application.Interfaces;

public interface IQuizService
{
    Task<Guid> InsertQuizAsync(Guid testId, string title, string? description, Guid subjectCode, string userEmail);
}