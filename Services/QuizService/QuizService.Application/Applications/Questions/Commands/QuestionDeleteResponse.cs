using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.Questions.Commands;

/// <summary>
/// Response cho delete question
/// </summary>
public record QuestionDeleteResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; } = null!;
}
