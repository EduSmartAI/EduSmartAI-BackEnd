using System.Text.Json;
using AuthService.Application.Accounts.Commands.Inserts;
using AuthService.Application.Accounts.Commands.Verifies;
using AuthService.Application.Consumers;
using AuthService.Application.Interfaces;
using AuthService.Domain.ReadModels;
using AuthService.Domain.Snapshort;
using AuthService.Domain.WriteModels;
using BaseService.Application.Interfaces.Commons;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;
using BuildingBlocks.Messaging.Events.InsertUserEvents;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Implements;

public class AccountService : IAccountService
{
    private readonly ICommandRepository<Account> _accountCommandRepository;
    private readonly ICommandRepository<Role> _roleCommandRepository;
    private readonly IQueryRepository<AccountCollection> _accountQueryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommandRepository<OutboxMessage> _outboxCommandRepository;
    private readonly ICommonLogic _commonLogic;

    public AccountService(
        ICommandRepository<Account> accountCommandRepository,
        ICommandRepository<Role> roleCommandRepository,
        IQueryRepository<AccountCollection> accountQueryRepository,
        IUnitOfWork unitOfWork,
        ICommonLogic commonLogic,
        ICommandRepository<OutboxMessage> outboxCommandRepository)
    {
        _accountCommandRepository = accountCommandRepository;
        _roleCommandRepository = roleCommandRepository;
        _accountQueryRepository = accountQueryRepository;
        _unitOfWork = unitOfWork;
        _commonLogic = commonLogic;
        _outboxCommandRepository = outboxCommandRepository;
    }

    /// <summary>
    /// Handles the insertion of a new student account.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<AccountInsertResponse> InsertAccountAsync(AccountInsertCommand request,
        CancellationToken cancellationToken)
    {
        var response = new AccountInsertResponse { Success = false };

        // Determine role based on email domain
        var role = await DetermineUserRoleAsync(request.Email, cancellationToken);

        // Check the existing account
        var existingAccount = await _accountCommandRepository.FirstOrDefaultAsync(x => x.Email == request.Email && x.IsActive, cancellationToken);
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
                    return await HandleAccountRecreationAsync(request, role, existingAccount, cancellationToken);
                }

                // If the account was created less than 5 minutes ago
                response.SetMessage(MessageId.E00000, "Xin đợi 5 phút trước khi tạo lại tài khoản");
                return response;
            }
        }

        // Begin transaction for inserting new account
        return await HandleNewAccountCreationAsync(request, role, null, cancellationToken);
    }

    /// <summary>
    /// Determines user role based on email domain
    /// </summary>
    private async Task<Role> DetermineUserRoleAsync(string email, CancellationToken cancellationToken)
    {
        if (email.Contains("@fe.edu.vn"))
        {
            return (await _roleCommandRepository.FirstOrDefaultAsync(
                x => x.Name == nameof(ConstantEnum.UserRole.Lecturer), cancellationToken))!;
        }
        
        return (await _roleCommandRepository.FirstOrDefaultAsync(
            x => x.Name == nameof(ConstantEnum.UserRole.Student), cancellationToken))!;
    }

    /// <summary>
    /// Handles account recreation when existing account is not confirmed
    /// </summary>
    private async Task<AccountInsertResponse> HandleAccountRecreationAsync(AccountInsertCommand request, Role role, Account existingAccount, CancellationToken cancellationToken)
    {
        var response = new AccountInsertResponse { Success = false };

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

            // Create and save outbox messages
            var success = await CreateAccountAndPublishEventsAsync(request, role, existingAccount.AccountId, cancellationToken);
            if (!success)
            {
                response.SetMessage(MessageId.E00000, "Có lỗi xảy ra khi tạo tài khoản");
                return false;
            }

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Đăng ký");
            return true;
        }, cancellationToken);

        return response;
    }

    /// <summary>
    /// Handles new account creation
    /// </summary>
    private async Task<AccountInsertResponse> HandleNewAccountCreationAsync(AccountInsertCommand request, Role role, Guid? oldUserId, CancellationToken cancellationToken)
    {
        var response = new AccountInsertResponse { Success = false };

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            var success = await CreateAccountAndPublishEventsAsync(request, role, oldUserId, cancellationToken);
            if (!success)
            {
                response.SetMessage(MessageId.E00000, "Có lỗi xảy ra khi tạo tài khoản");
                return false;
            }

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Đăng ký");
            return true;
        }, cancellationToken);

        return response;
    }

    /// <summary>
    /// Creates account and publishes events to outbox
    /// </summary>
    private async Task<bool> CreateAccountAndPublishEventsAsync(AccountInsertCommand request, Role role, Guid? oldUserId, CancellationToken cancellationToken)
    {
        // Insert new account
        var newUser = await CreateAccountAsync(request, role.Id);
        if (newUser == null)
        {
            return false;
        }

        var outboxMessages = new List<OutboxMessage>();

        // Add user insert event based on role
        var userInsertMessage = CreateUserInsertEventMessage(newUser, request, role, oldUserId);
        outboxMessages.Add(userInsertMessage);

        // Add send key event
        var sendKeyMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(SendKeyEvent),
            Content = JsonSerializer.Serialize(new SendKeyEvent { Key = newUser.Key!, Email = newUser.Email }),
            OccurredOnUtc = DateTime.UtcNow,
        };
        outboxMessages.Add(sendKeyMessage);

        // Add account collection event
        var userInformation = new UserInformation
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
        };

        var accountCollection = AccountCollection.FromWriteModel(newUser, userInformation);
        var accountCollectionEvent = new AccountCollectionEvent
        {
            Account = accountCollection
        };

        var accountOutboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(AccountCollectionEvent),
            Content = JsonSerializer.Serialize(accountCollectionEvent),
            OccurredOnUtc = DateTime.UtcNow,
        };
        outboxMessages.Add(accountOutboxMessage);

        // Save all outbox messages
        await _outboxCommandRepository.AddRangeAsync(outboxMessages);
        await _unitOfWork.SaveChangesAsync(request.Email, cancellationToken);

        return true;
    }

    /// <summary>
    /// Creates outbox message for user insert event based on role
    /// </summary>
    private OutboxMessage CreateUserInsertEventMessage(Account newUser, AccountInsertCommand request, Role role, Guid? oldUserId)
    {
        var isLecturer = role.Name == nameof(ConstantEnum.UserRole.Lecturer);

        if (isLecturer)
        {
            var lecturerInsertEvent = new LecturerInsertEvent
            {
                UserId = newUser.AccountId,
                OldUserId = oldUserId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = newUser.Email,
            };

            return new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(LecturerInsertEvent),
                Content = JsonSerializer.Serialize(lecturerInsertEvent),
                OccurredOnUtc = DateTime.UtcNow,
            };
        }
        else
        {
            var studentEvent = new StudentInsertEvent
            {
                UserId = newUser.AccountId,
                OldUserId = oldUserId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserRole = (byte)ConstantEnum.UserRole.Student,
                Email = newUser.Email,
            };

            return new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(StudentInsertEvent),
                Content = JsonSerializer.Serialize(studentEvent),
                OccurredOnUtc = DateTime.UtcNow,
            };
        }
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

        var accountCollection =
            await _accountQueryRepository.FirstOrDefaultAsync(x => x.AccountId == account.AccountId);

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
    private async Task<Account?> CreateAccountAsync(AccountInsertCommand request, Guid roleId)
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