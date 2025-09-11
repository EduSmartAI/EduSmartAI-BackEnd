using QuizService.Domain.WriteModels;

namespace QuizService.Domain.ReadModels;

public sealed class QuestionCollection
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = null!;
    public bool IsActive { get; set; }
    public ICollection<AnswerCollection> Answers { get; set; } = new List<AnswerCollection>();

    public static QuestionCollection FromWriteModel(Question model)
    {
        var question = new QuestionCollection
        {
            QuestionId = model.QuestionId,
            QuestionText = model.QuestionText,
            IsActive = model.IsActive
        };

        if (model.Answers.Any())
        {
            question.Answers = model.Answers.Select(AnswerCollection.FromWriteModel).ToList();
        }

        return question;
    }
}