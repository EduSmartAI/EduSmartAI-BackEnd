using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.StudentSurveys.Commands;

public record StudentSurveyInsertResponse() : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}