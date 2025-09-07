using AuthService.Application.Interfaces;
using BuildingBlocks.CQRS;

namespace AuthService.Application.Accounts.Commands.Inserts;

public class StudentInsertCommandHandler : ICommandHandler<StudentInsertCommand, StudentInsertResponse>
{
    private readonly IAccountService _accountService;

    public StudentInsertCommandHandler(IAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// Handles the insertion of a new account.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentInsertResponse> Handle(StudentInsertCommand request, CancellationToken cancellationToken)
    {
        return await _accountService.InsertStudentAsync(request, cancellationToken);
    }
}