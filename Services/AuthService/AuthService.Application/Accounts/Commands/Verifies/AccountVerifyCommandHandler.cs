using AuthService.Application.Interfaces;
using BuildingBlocks.CQRS;

namespace AuthService.Application.Accounts.Commands.Verifies;

public class AccountVerifyCommandHandler : ICommandHandler<AccountVerifyCommand, AccountVerifyResponse>
{
    private readonly IAccountService _accountService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="accountService"></param>
    public AccountVerifyCommandHandler(IAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// Handles account verification by validating the provided key
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<AccountVerifyResponse> Handle(AccountVerifyCommand request, CancellationToken cancellationToken)
    {
        return await _accountService.VerifyAccount(request.Key);
    }
}