using QuizService.Domain.WriteModels;

namespace QuizService.Domain.ReadModels;

public sealed class StudentTestCollection
{
    public Guid StudentTestId { get; set; }
    public Guid StudentId { get; set; }
    public Guid TestId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;
    public bool IsActive { get; set; }
    public ICollection<StudentAnswerCollection> StudentAnswers { get; set; } = new List<StudentAnswerCollection>();

    public static StudentTestCollection FromWriteModel(StudentTest model)
    {
        var st = new StudentTestCollection
        {
            StudentTestId = model.StudentTestId,
            StudentId = model.StudentId,
            TestId = model.TestId,
            StartedAt = model.StartedAt,
            FinishedAt = model.FinishedAt,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };

        if (model.StudentAnswers.Any())
        {
            st.StudentAnswers = model.StudentAnswers.Select(StudentAnswerCollection.FromWriteModel).ToList();
        }

        return st;
    }
}
