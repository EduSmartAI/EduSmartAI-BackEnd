using AuthService.Application.Interfaces;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.AuthService.UserLoginEvents;
using BuildingBlocks.Messaging.Events.UserLoginEvents;
using MassTransit;

namespace AuthService.Application.Accounts.Commands.Logins;

public class UserLoginCommandHandler : ICommandHandler<UserLoginCommand, UserLoginResponse>
{
    private readonly IAccountService _accountService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestClient<StudentLoginEvent> _studentRequestClient;
    private readonly IRequestClient<TeacherLoginEvent> _teacherRequestClient;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="accountService"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="studentRequestClient"></param>
    /// <param name="teacherRequestClient"></param>
    public UserLoginCommandHandler(
        IAccountService accountService, 
        IUnitOfWork unitOfWork, 
        IRequestClient<StudentLoginEvent> studentRequestClient,
        IRequestClient<TeacherLoginEvent> teacherRequestClient)
    {
        _accountService = accountService;
        _unitOfWork = unitOfWork;
        _studentRequestClient = studentRequestClient;
        _teacherRequestClient = teacherRequestClient;
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

            // Send login event based on role
            UserLoginEntity? userLoginEntity;
            
            if (roleName == nameof(ConstantEnum.UserRole.Lecturer))
            {
                // Send login event to TeacherService
                var teacherEvent = new TeacherLoginEvent { UserId = account.AccountId };
                var teacherLoginResponse = await _teacherRequestClient.GetResponse<TeacherLoginEventResponse>(teacherEvent, cancellationToken);
                
                if (!teacherLoginResponse.Message.Success)
                {
                    response.SetMessage(teacherLoginResponse.Message.MessageId, teacherLoginResponse.Message.Message);
                    return false;
                }
                
                var teacherMsg = teacherLoginResponse.Message.Response;
                userLoginEntity = new UserLoginEntity(
                    UserId: account.AccountId,
                    FullName: $"{teacherMsg.FirstName} {teacherMsg.LastName}",
                    Email: account.Email,
                    RoleName: roleName,
                    AvatarUrl: teacherMsg.AvatarUrl
                );
            }
            else if (roleName == nameof(ConstantEnum.UserRole.Student))
            {
                // Send login event to StudentService
                var studentEvent = new StudentLoginEvent { UserId = account.AccountId };
                var studentLoginResponse = await _studentRequestClient.GetResponse<StudentLoginEventResponse>(studentEvent, cancellationToken);
                
                if (!studentLoginResponse.Message.Success)
                {
                    response.SetMessage(studentLoginResponse.Message.MessageId, studentLoginResponse.Message.Message);
                    return false;
                }
                
                var studentMsg = studentLoginResponse.Message.Response;
                userLoginEntity = new UserLoginEntity(
                    UserId: account.AccountId,
                    FullName: $"{studentMsg.FirstName} {studentMsg.LastName}",
                    Email: account.Email,
                    RoleName: roleName,
                    AvatarUrl: studentMsg.AvatarUrl
                );
            }
            else if (roleName == nameof(ConstantEnum.UserRole.Admin))
            {
                var adminAccount = await _accountService.GetAdminAccountByIdAsync(account.AccountId, cancellationToken);
                if (adminAccount == null)
                {
                    response.SetMessage(MessageId.E00000, "Không tìm thấy tài khoản");
                    return false;
                }
                userLoginEntity = new UserLoginEntity(
                    UserId: account.AccountId,
                    FullName: adminAccount.FullName,
                    Email: account.Email,
                    RoleName: roleName,
                    AvatarUrl: null
                );
            }
            else
            {
                response.SetMessage(MessageId.E99999);
                return false;
            }

            // Reset attempts
            _accountService.ResetFailedAttempts(account);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            response.Response = userLoginEntity;

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Đăng nhập");
            return true;
        }, cancellationToken);
        return response;
    }
}