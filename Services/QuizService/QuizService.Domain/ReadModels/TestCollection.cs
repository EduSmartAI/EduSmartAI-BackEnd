using QuizService.Domain.WriteModels;

namespace QuizService.Domain.ReadModels;

public sealed class TestCollection
{
    public Guid TestId { get; set; }
    public string TestName { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;
    public bool IsActive { get; set; }

    public ICollection<QuizCollection> Quizzes { get; set; } = new List<QuizCollection>();

    public static TestCollection FromWriteModel(Test model)
    {
        var test = new TestCollection
        {
            TestId = model.TestId,
            TestName = model.TestName,
            Description = model.Description,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };

        if (model.Quizzes.Any())
        {
            test.Quizzes = model.Quizzes.Select(QuizCollection.FromWriteModel).ToList();
        }

        return test;
    }
}
