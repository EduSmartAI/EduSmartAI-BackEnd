using AuthService.Application.Accounts.Commands.Inserts;
using AuthService.Application.Accounts.Commands.Verifies;
using AuthService.Application.Interfaces;
using AuthService.Domain.ReadModels;
using AuthService.Domain.Snapshort;
using AuthService.Domain.WriteModels;
using BaseService.Application.Interfaces.Commons;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.InsertUserEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Implements;

public class AccountService : IAccountService
{
    private readonly ICommandRepository<Account> _accountCommandRepository;
    private readonly ICommandRepository<Role> _roleCommandRepository;
    private readonly IQueryRepository<AccountCollection> _accountQueryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestClient<UserInsertEvent> _requestUserClient;
    private readonly IPublishEndpoint _requestPublishEndpoint; 
    private readonly ICommonLogic _commonLogic;

    public AccountService(
        ICommandRepository<Account> accountCommandRepository,
        ICommandRepository<Role> roleCommandRepository,
        IQueryRepository<AccountCollection> accountQueryRepository,
        IUnitOfWork unitOfWork, IRequestClient<UserInsertEvent> requestUserClient,
        ICommonLogic commonLogic, IPublishEndpoint requestPublishEndpoint)
    {
        _accountCommandRepository = accountCommandRepository;
        _roleCommandRepository = roleCommandRepository;
        _accountQueryRepository = accountQueryRepository;
        _unitOfWork = unitOfWork;
        _requestUserClient = requestUserClient;
        _commonLogic = commonLogic;
        _requestPublishEndpoint = requestPublishEndpoint;
    }

    /// <summary>
    /// Handles the insertion of a new student account.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentInsertResponse> InsertStudentAsync(StudentInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new StudentInsertResponse { Success = false };

        // Check role existence
        var role = await _roleCommandRepository.FirstOrDefaultAsync(
            x => x.Name == nameof(ConstantEnum.UserRole.Student), cancellationToken);
        if (role == null)
        {
            response.SetMessage(MessageId.E99999);
            return response;
        }

        // Check the existing account
        var existingAccount = await _accountCommandRepository.FirstOrDefaultAsync(
            x => x.Email == request.Email && x.IsActive,
            cancellationToken);

        if (existingAccount != null)
        {
            if (existingAccount.EmailConfirmed && existingAccount.IsActive)
            {
                response.SetMessage(MessageId.E00000, "Email đã tồn tại.");
                return response;
            }

            // Delete and recreate the account if email is not confirmed and created more than 5 minutes ago
            if (!existingAccount.EmailConfirmed)
            {
                // If the account was created more than 5 minutes ago
                if (existingAccount.CreatedAt < DateTime.UtcNow.AddMinutes(-5))
                {
                    await _unitOfWork.BeginTransactionAsync(async () =>
                    {
                        // Deactivate existing account
                        _accountCommandRepository.Update(existingAccount, existingAccount.Email, true);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);

                        var userCollectionExisting = await _accountQueryRepository.FirstOrDefaultAsync(x => x.Email == request.Email && x.IsActive);
                        if (userCollectionExisting != null)
                        {
                            // Delete from AccountCollection
                            _unitOfWork.Delete(AccountCollection.FromWriteModel(existingAccount, userCollectionExisting.UserInformation));
                            await _unitOfWork.SessionSaveChangesAsync();
                        }

                        // Insert new account
                        var newUser = await InsertAccountAsync(request, role.Id);
                        if (newUser == null)
                        {
                            response.SetMessage(MessageId.E00000, "Có lỗi xảy ra khi tạo tài khoản");
                            return false;
                        }

                        // Insert into User service
                        var @event = new UserInsertEvent
                        {
                            UserId = newUser.AccountId,
                            OldUserId = existingAccount.AccountId,
                            FirstName = request.FirstName,
                            LastName = request.LastName,
                            UserRole = (byte)ConstantEnum.UserRole.Student,
                            Email = newUser.Email,
                        };

                        var userInsertResponse = await _requestUserClient.GetResponse<UserInsertEventResponse>(@event, cancellationToken);
                        if (!userInsertResponse.Message.Success)
                        {
                            response.SetMessage(MessageId.E00000, userInsertResponse.Message.Message);
                            return false;
                        }

                        // True
                        response.Success = true;
                        response.SetMessage(MessageId.I00001, "Đăng ký");
                        return true;
                    }, cancellationToken);

                    return response;
                }

                // If the account was created less than 5 minutes ago
                response.SetMessage(MessageId.E00000, "Xin đợi 5 phút trước khi tạo lại tài khoản");
                return response;
            }
        }

        // Begin transaction for inserting new account
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            var newAccount = await InsertAccountAsync(request, role.Id);
            if (newAccount == null)
            {
                response.SetMessage(MessageId.E00000 , "Có lỗi xảy ra khi tạo tài khoản");
                return false;
            }

            var @userInsertEvent = new UserInsertEvent
            {
                UserId = newAccount.AccountId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserRole = (byte)ConstantEnum.UserRole.Student,
                Email = newAccount.Email,
            };

            var userInsertResponse = await _requestUserClient.GetResponse<UserInsertEventResponse>(@userInsertEvent, cancellationToken);
            if (!userInsertResponse.Message.Success)
            {
                response.SetMessage(MessageId.E00000, userInsertResponse.Message.Message);
                return false;
            }

            var userInformation = new UserInformation
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
            };

            // Send key to email user
            await _requestPublishEndpoint.Publish(new SendKeyEvent { Key = newAccount.Key!, Email = newAccount.Email}, cancellationToken);

            _unitOfWork.Store(AccountCollection.FromWriteModel(newAccount, userInformation));
            await _unitOfWork.SessionSaveChangesAsync();

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Đăng ký");
            return true;
        }, cancellationToken);

        return response;
    }

    /// <summary>
    /// Validates user by email,
    /// checks if account exists,
    /// is active, email confirmed, and not locked.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(bool Success, string MessageId, Account? Account)> ValidateUserAsync(string email,
        CancellationToken cancellationToken)
    {
        // Check if the account exists, is active, and email is confirmed
        var account = await _accountCommandRepository
            .FirstOrDefaultAsync(x => x.Email == email && x.IsActive && x.EmailConfirmed, cancellationToken);

        // The Account does not exist, or is not active or email not confirmed
        if (account == null) return (false, MessageId.E11005, null);

        // Check if the account is locked
        if (account.LockoutEnd.HasValue && account.LockoutEnd > DateTimeOffset.Now)
            return (false, MessageId.E11003, null);

        return (true, string.Empty, account);
    }

    /// <summary>
    /// Check if the provided password matches the stored password for the given account.
    /// </summary>
    /// <param name="account"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public bool CheckPassword(Account account, string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, account.PasswordHash);
    }

    /// <summary>
    /// Locks the account after a certain number of failed login attempts.
    /// </summary>
    /// <param name="account"></param>
    public void LockAccount(Account account)
    {
        account.AccessFailedCount++;
        if (account.AccessFailedCount >= 5)
            account.LockoutEnd = DateTimeOffset.Now + TimeSpan.FromMinutes(5);

        _accountCommandRepository.Update(account, account.Email);
    }

    /// <summary>
    /// Resets the failed login attempts counter after a successful login.
    /// </summary>
    /// <param name="account"></param>
    public void ResetFailedAttempts(Account account)
    {
        account.AccessFailedCount = 0;
        account.LockoutEnd = null;
        _accountCommandRepository.Update(account, account.Email);
    }

    /// <summary>
    /// Retrieves the role details based on the role name.
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string?> GetUserRoleNameAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return await _roleCommandRepository
            .Find(x => x.Id == roleId)
            .Select(x => x!.Name)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Verifies the account using the provided request key.
    /// </summary>
    /// <param name="requestKey"></param>
    /// <returns></returns>
    public async Task<AccountVerifyResponse> VerifyAccount(string requestKey)
    {
        var response = new AccountVerifyResponse { Success = false };
        
        var emailDecrypted = _commonLogic.DecryptText(requestKey);
        if (!emailDecrypted.Success)
        {
            response.SetMessage(MessageId.E00000, "Liên kết xác nhận không hợp lệ hoặc đã hết hạn.");
            return response;
        }

        // Find the account by the provided key
        var account = await _accountCommandRepository.FirstOrDefaultAsync(x => x.Key == requestKey && x.IsActive);
        if (account == null)
        {
            response.SetMessage(MessageId.E00000, "Liên kết không hợp lệ hoặc đã hết hạn");
            return response;
        }
        
        // Check if the key is expired (5 minutes)
        if (account.CreatedAt.AddMinutes(5) < DateTime.UtcNow)
        {
            response.SetMessage(MessageId.E00000, "Liên kết không hợp lệ hoặc đã hết hạn");
            return response;
        }

        // Check if the email is already confirmed
        if (account.EmailConfirmed)
        {
            response.SetMessage(MessageId.I00001, "Tài khoản đã được xác nhận trước đó.");
            response.Success = true;
            return response;
        }
        
        var accountCollection = await _accountQueryRepository.FirstOrDefaultAsync(x => x.AccountId == account.AccountId);

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Update the account
            account.EmailConfirmed = true;
            account.Key = null;
        
            accountCollection!.EmailConfirmed = true;
            accountCollection.Key = null;
        
            _accountCommandRepository.Update(account, account.Email);
            await _unitOfWork.SaveChangesAsync();

            _unitOfWork.Store(accountCollection);
            await _unitOfWork.SessionSaveChangesAsync();
        
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Xác nhận email");
            return true;
        });
        return response;
    }

    /// <summary>
    /// Inserts a new account into the database.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="roleId"></param>
    /// <returns></returns>
    private async Task<Account?> InsertAccountAsync(StudentInsertCommand request, Guid roleId)
    {
        var accountId = Guid.NewGuid();
        var key = _commonLogic.EncryptText($"{request.Email}-{accountId}");
        if (!key.Success)
        {
            return null;
        }
        var newAccount = new Account
        {
            AccountId = accountId,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            EmailConfirmed = false,
            Key = key.Response.EncryptedKey,
            RoleId = roleId,
        };

        await _accountCommandRepository.AddAsync(newAccount, request.Email);
        await _unitOfWork.SaveChangesAsync();

        return newAccount;
    }
}