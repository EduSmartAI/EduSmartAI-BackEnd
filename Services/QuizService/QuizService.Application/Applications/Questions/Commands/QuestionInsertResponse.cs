using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.Questions.Commands;

/// <summary>
/// Response cho insert question với answers
/// </summary>
public record QuestionInsertResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}
