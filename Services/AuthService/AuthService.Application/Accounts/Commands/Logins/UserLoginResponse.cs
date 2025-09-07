using BaseService.Common.ApiEntities;

namespace AuthService.Application.Accounts.Commands.Logins;

public record UserLoginResponse : AbstractApiResponse<UserLoginEntity>
{
    public override UserLoginEntity Response { get; set; }
}

public record UserLoginEntity(
    Guid UserId,
    string FullName,
    string Email,
    string RoleName
);