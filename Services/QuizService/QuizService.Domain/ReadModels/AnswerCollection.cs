using QuizService.Domain.WriteModels;

namespace QuizService.Domain.ReadModels;

public sealed class AnswerCollection
{
    public Guid AnswerId { get; set; }
    public string AnswerText { get; set; } = null!;
    public bool? IsCorrect { get; set; }
    public bool IsActive { get; set; }

    public static AnswerCollection FromWriteModel(Answer model)
    {
        return new AnswerCollection
        {
            AnswerId = model.AnswerId,
            AnswerText = model.AnswerText,
            IsCorrect = model.IsCorrect,
            IsActive = model.IsActive
        };
    }
}
