using QuizService.Domain.WriteModels;

namespace QuizService.Domain.ReadModels;

public sealed class QuizCollection
{
    public Guid QuizId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public Guid SubjectCode { get; set; }
    public bool IsActive { get; set; }
    public ICollection<QuestionCollection> Questions { get; set; } = new List<QuestionCollection>();

    public static QuizCollection FromWriteModel(Quiz model)
    {
        var quiz = new QuizCollection
        {
            QuizId = model.QuizId,
            Title = model.Title,
            Description = model.Description,
            SubjectCode = model.SubjectCode,
            IsActive = model.IsActive
        };

        if (model.Questions.Any())
        {
            quiz.Questions = model.Questions.Select(QuestionCollection.FromWriteModel).ToList();
        }

        return quiz;
    }
}