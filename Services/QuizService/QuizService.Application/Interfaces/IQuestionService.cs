namespace QuizService.Application.Interfaces;

public interface IQuestionService
{
    Task<Guid> InsertQuestionAsync(Guid quizId, string text, short questionType);

}