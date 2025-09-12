using QuizService.Domain.WriteModels;

namespace QuizService.Domain.ReadModels;

public sealed class AnswerCollection
{
    public Guid AnswerId { get; set; }
    public Guid QuestionId { get; set; }
    public string AnswerText { get; set; } = null!;
    public bool? IsCorrect { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public static AnswerCollection FromWriteModel(Answer model)
    {
        return new AnswerCollection
        {
            AnswerId = model.AnswerId,
            QuestionId = model.QuestionId,
            AnswerText = model.AnswerText,
            IsCorrect = model.IsCorrect,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };
    }
}
