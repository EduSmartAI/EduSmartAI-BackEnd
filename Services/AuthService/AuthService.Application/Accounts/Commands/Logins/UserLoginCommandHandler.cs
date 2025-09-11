using AuthService.Application.Interfaces;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.UserLoginEvents;
using MassTransit;

namespace AuthService.Application.Accounts.Commands.Logins;

public class UserLoginCommandHandler : ICommandHandler<UserLoginCommand, UserLoginResponse>
{
    private readonly IAccountService _accountService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestClient<UserLoginEvent> _requestClient;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="accountService"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="requestClient"></param>
    public UserLoginCommandHandler(IAccountService accountService, IUnitOfWork unitOfWork, IRequestClient<UserLoginEvent> requestClient)
    {
        _accountService = accountService;
        _unitOfWork = unitOfWork;
        _requestClient = requestClient;
    }

    /// <summary>
    /// Handles user login by validating credentials,
    /// checking roles,
    /// and sending login events.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<UserLoginResponse> Handle(UserLoginCommand request, CancellationToken cancellationToken)
    {
        var response = new UserLoginResponse { Success = false };
        
        var (valid, messageId, account) = await _accountService.ValidateUserAsync(request.UserName!, cancellationToken);
        if (!valid || account == null || !string.IsNullOrEmpty(messageId))
        {
            response.SetMessage(messageId);
            return response;
        }
        
        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Check password
            if (!_accountService.CheckPassword(account, request.Password!))
            {
                _accountService.LockAccount(account);
                response.SetMessage(MessageId.E11002);
                return false;
            }

            // Get the role of the user
            var roleName = await _accountService.GetUserRoleNameAsync(account.RoleId, cancellationToken);
            if (string.IsNullOrEmpty(roleName))
            {
                response.SetMessage(MessageId.E99999);
                return false;
            }

            // Send login event
            var @event = new UserLoginEvent { UserId = account.AccountId };
            var userLoginMessageResponse = await _requestClient.GetResponse<UserLoginEventResponse>(@event, cancellationToken);
            if (!userLoginMessageResponse.Message.Success)
            {
                response.SetMessage(userLoginMessageResponse.Message.MessageId, userLoginMessageResponse.Message.Message);
                return false;
            }

            // Reset attempts
            _accountService.ResetFailedAttempts(account);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var msg = userLoginMessageResponse.Message.Response;
            response.Response = new UserLoginEntity(
                UserId: account.AccountId,
                FullName: $"{msg.FirstName} {msg.LastName}",
                Email: account.Email,
                RoleName: roleName
            );

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Đăng nhập");
            return true;
        }, cancellationToken);
        return response;
    }
}