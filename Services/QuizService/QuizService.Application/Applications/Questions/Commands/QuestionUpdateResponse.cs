using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.Questions.Commands;

/// <summary>
/// Response cho update question
/// </summary>
public record QuestionUpdateResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; } = null!;
}
