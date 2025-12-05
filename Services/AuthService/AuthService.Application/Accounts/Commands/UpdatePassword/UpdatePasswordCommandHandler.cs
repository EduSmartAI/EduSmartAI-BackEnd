using AuthService.Domain.WriteModels;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;

namespace AuthService.Application.Accounts.Commands.UpdatePassword;

public class UpdatePasswordCommandHandler(
    ICommandRepository<Account> accountRepo, 
    IUnitOfWork unitOfWork, 
    IIdentityService identityService) 
    : ICommandHandler<UpdatePasswordCommand, UpdatePasswordResponse>
{
    public async Task<UpdatePasswordResponse> Handle(UpdatePasswordCommand request, CancellationToken cancellationToken)
    {
        var response = new UpdatePasswordResponse { Success = false };

        try
        {
            // Get current user from token
            var currentUser = identityService.GetCurrentUser();
            if (currentUser == null)
            {
                response.SetMessage(MessageId.E00000, "Không tìm thấy thông tin người dùng");
                return response;
            }

            // Get account
            var account = await accountRepo
                .FirstOrDefaultAsync(
                    predicate: a => a.Email == currentUser.Email && a.IsActive,
                    cancellationToken: cancellationToken);
            
            if (account == null)
            {
                response.SetMessage(MessageId.E00000, "Tài khoản không tồn tại");
                return response;
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, account.PasswordHash))
            {
                response.SetMessage(MessageId.E00000, "Mật khẩu hiện tại không đúng");
                return response;
            }

            // Check if new password is same as current password
            if (BCrypt.Net.BCrypt.Verify(request.NewPassword, account.PasswordHash))
            {
                response.SetMessage(MessageId.E00000, "Mật khẩu mới không được trùng với mật khẩu hiện tại");
                return response;
            }

            // Update new password
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            accountRepo.Update(account);
            await unitOfWork.SaveChangesAsync(account.Email, cancellationToken);

            // Success
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Đổi mật khẩu");
            return response;
        }
        catch (Exception e)
        {
            response.SetMessage(MessageId.E00000, $"Lỗi khi đổi mật khẩu: {e.Message}");
            return response;
        }
    }
}

