using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Questions.Commands;

/// <summary>
/// Command để delete question và tất cả answers của nó
/// </summary>
public record QuestionDeleteCommand : ICommand<QuestionDeleteResponse>
{
    /// <summary>
    /// ID của question cần delete
    /// </summary>
    public Guid QuestionId { get; set; }
}
