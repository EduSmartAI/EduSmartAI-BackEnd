using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Questions.Commands;

/// <summary>
/// Command để insert question và answers vào quiz
/// </summary>
public record QuestionInsertCommand : ICommand<QuestionInsertResponse>
{
    /// <summary>
    /// ID của quiz để thêm question vào
    /// </summary>
    public Guid QuizId { get; set; }

    /// <summary>
    /// Nội dung câu hỏi
    /// </summary>
    public string QuestionText { get; set; } = null!;

    /// <summary>
    /// Danh sách câu trả lời của question
    /// </summary>
    public List<InsertAnswers> Answers { get; set; } = null!;
}

/// <summary>
/// Model cho insert answers
/// </summary>
public record InsertAnswers
{
    /// <summary>
    /// Nội dung câu trả lời
    /// </summary>
    public string AnswerText { get; set; } = null!;

    /// <summary>
    /// Đáp án đúng hay sai
    /// </summary>
    public bool? IsCorrect { get; set; }
}
