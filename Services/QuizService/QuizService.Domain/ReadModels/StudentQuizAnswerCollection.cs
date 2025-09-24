using QuizService.Domain.WriteModels;

namespace QuizService.Domain.ReadModels;

public class StudentQuizAnswerCollection
{
    public Guid StudentQuizAnswerId { get; set; }

    public Guid StudentQuizId { get; set; }

    public Guid QuestionId { get; set; }

    public Guid AnswerId { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }
    
    public AnswerCollection? Answer { get; set; }
    
    public QuestionCollection? Question { get; set; }
    
    public static StudentQuizAnswerCollection FromWriteModel(StudentQuizAnswer model)
    {
        var result = new StudentQuizAnswerCollection
        {
            StudentQuizAnswerId = model.StudentQuizAnswerId,
            StudentQuizId = model.StudentQuizId,
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

        result.Question = QuestionCollection.FromWriteModel(model.Question);
        return result;
    }
}