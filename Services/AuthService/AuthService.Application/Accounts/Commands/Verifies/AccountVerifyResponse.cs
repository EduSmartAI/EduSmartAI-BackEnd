using BaseService.Common.ApiEntities;

namespace AuthService.Application.Accounts.Commands.Verifies;

public record AccountVerifyResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}