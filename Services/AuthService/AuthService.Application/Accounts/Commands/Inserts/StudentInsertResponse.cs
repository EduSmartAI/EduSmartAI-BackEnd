using BaseService.Common.ApiEntities;

namespace AuthService.Application.Accounts.Commands.Inserts;

public record StudentInsertResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}