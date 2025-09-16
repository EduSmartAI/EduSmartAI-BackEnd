using QuizService.Domain.WriteModels;

namespace QuizService.Domain.ReadModels;

public class StudentQuizCollection
{
    public Guid StudentQuizId { get; set; }

    public Guid StudentId { get; set; }

    public Guid QuizId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }
    
    public QuizCollection Quiz { get; set; }

    public virtual ICollection<StudentQuizAnswerCollection> StudentQuizAnswers { get; set; } = new List<StudentQuizAnswerCollection>();
    
    public static StudentQuizCollection FromWriteModel(StudentQuiz model)
    {
        var result = new StudentQuizCollection
        {
            StudentQuizId = model.StudentQuizId,
            StudentId = model.StudentId,
            QuizId = model.QuizId,
            IsActive = model.IsActive,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
        };
        result.Quiz = QuizCollection.FromWriteModel(model.Quiz);
        foreach (var answer in model.StudentQuizAnswers)
        {
            result.StudentQuizAnswers.Add(StudentQuizAnswerCollection.FromWriteModel(answer));
        }
        return result;
    }
    
    public static StudentQuizCollection FromWriteModel(StudentQuiz studentQuiz, QuizCollection quiz)
    {
        var result = new StudentQuizCollection
        {
            StudentQuizId = studentQuiz.StudentQuizId,
            StudentId = studentQuiz.StudentId,
            QuizId = studentQuiz.QuizId,
            IsActive = studentQuiz.IsActive,
            CreatedAt = studentQuiz.CreatedAt,
            UpdatedAt = studentQuiz.UpdatedAt,
            CreatedBy = studentQuiz.CreatedBy,
            UpdatedBy = studentQuiz.UpdatedBy,
            Quiz = quiz
        };
        foreach (var answer in studentQuiz.StudentQuizAnswers)
        {
            result.StudentQuizAnswers.Add(StudentQuizAnswerCollection.FromWriteModel(answer));
        }
        return result;
    }
}