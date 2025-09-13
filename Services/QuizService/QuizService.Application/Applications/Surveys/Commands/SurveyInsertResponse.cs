using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.Surveys.Commands;

public record SurveyInsertResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}