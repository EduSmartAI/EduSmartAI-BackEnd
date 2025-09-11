using QuizService.Domain.WriteModels;

namespace QuizService.Domain.ReadModels;

public sealed class StudentAnswerCollection
{
    public Guid StudentAnswerId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid? AnswerId { get; set; }
    
    public bool IsActive { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;
    
    public AnswerCollection? Answer { get; set; }

    public static StudentAnswerCollection FromWriteModel(StudentAnswer model)
    {
        var result =  new StudentAnswerCollection
        {
            StudentAnswerId = model.StudentAnswerId,
            QuestionId = model.QuestionId,
            AnswerId = model.AnswerId,
            IsActive = model.IsActive,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
        };
        if (model.Answer != null)
        {
            result.Answer = AnswerCollection.FromWriteModel(model.Answer);
        }
        return result;
    }
}