namespace QuizService.Application.Interfaces;

public interface IAnswerService
{
    Task InsertAnswerAsync(Guid questionId, string text, bool isCorrect, string email);
}