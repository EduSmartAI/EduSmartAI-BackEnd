using AuthService.Application.Accounts.Commands.Inserts;
using AuthService.Application.Accounts.Commands.Verifies;
using AuthService.Domain.WriteModels;

namespace AuthService.Application.Interfaces;

public interface IAccountService
{
    Task<StudentInsertResponse> InsertStudentAsync(StudentInsertCommand request, CancellationToken cancellationToken);

    /// <summary>
    /// Validates user by email,
    /// checks if account exists,
    /// is active, email confirmed, and not locked.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<(bool Success, string MessageId, Account? Account)> ValidateUserAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Check if the provided password matches the stored password for the given account.
    /// </summary>
    /// <param name="account"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    bool CheckPassword(Account account, string password);

    /// <summary>
    /// Locks the account after a certain number of failed login attempts.
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    void LockAccount(Account account);
    
    /// <summary>
    /// Resets the failed login attempts counter after a successful login.
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    void ResetFailedAttempts(Account account);
    
    /// <summary>
    /// Retrieves the role details based on the role name.
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<string?> GetUserRoleNameAsync(Guid roleId, CancellationToken cancellationToken);

    /// <summary>
    /// Verifies the account using the provided request key.
    /// </summary>
    /// <param name="requestKey"></param>
    /// <returns></returns>
    Task<AccountVerifyResponse> VerifyAccount(string requestKey);
}