using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.Tests.Commands;

public record TestInsertResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}