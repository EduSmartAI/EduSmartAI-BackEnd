using QuizService.Domain.WriteModels;

namespace QuizService.Domain.ReadModels;

public sealed class StudentAnswerCollection
{
    public Guid StudentAnswerId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid? AnswerId { get; set; }
    public string? AnswerText { get; set; }
    public bool IsActive { get; set; }

    public static StudentAnswerCollection FromWriteModel(StudentAnswer model)
    {
        return new StudentAnswerCollection
        {
            StudentAnswerId = model.StudentAnswerId,
            QuestionId = model.QuestionId,
            AnswerId = model.AnswerId,
            AnswerText = model.AnswerText,
            IsActive = model.IsActive
        };
    }
}