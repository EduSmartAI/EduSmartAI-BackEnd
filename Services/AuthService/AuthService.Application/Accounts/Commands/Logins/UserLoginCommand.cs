using BuildingBlocks.CQRS;

namespace AuthService.Application.Accounts.Commands.Logins;

public record UserLoginCommand(string? UserName, string? Password) : ICommand<UserLoginResponse>;