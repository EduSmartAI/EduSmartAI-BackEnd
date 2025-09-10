using QuizService.Domain.WriteModels;

namespace QuizService.Domain.ReadModels;

public sealed class StudentTestCollection
{
    public Guid StudentTestId { get; set; }
    public Guid StudentId { get; set; }
    public Guid TestId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
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
            IsActive = model.IsActive
        };

        if (model.StudentAnswers.Any())
        {
            st.StudentAnswers = model.StudentAnswers.Select(StudentAnswerCollection.FromWriteModel).ToList();
        }

        return st;
    }
}
