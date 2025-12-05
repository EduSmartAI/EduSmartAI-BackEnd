using AuthService.Domain.WriteModels;
using BaseService.Application.Interfaces.Commons;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;

namespace AuthService.Application.Accounts.Commands.ForgotPassword;

public class ResetPasswordCommandHandler(ICommandRepository<Account> accountRepo, IUnitOfWork unitOfWork, ICommonLogic commonLogic) : ICommandHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var response = new ResetPasswordResponse { Success = false };

        try
        {
            var decryptedKey = commonLogic.DecryptTextDateTimeAndEmail(request.Key);
            if (decryptedKey.Response.DateTimeValue < DateTime.Now.AddMinutes(-5))
            {
                response.SetMessage(MessageId.E00000, "Liên kết hết hạn. Vui lòng thử lại.");
                return response;
            }
        
            var email = decryptedKey.Response.Email;

            var account = await accountRepo
                .FirstOrDefaultAsync(
                    predicate: a => a.Email == email && a.Key == request.Key && a.IsActive,
                    cancellationToken: cancellationToken);
            if (account == null)
            {
                response.SetMessage(MessageId.E00000, "Link không hợp lệ hoặc đã hết hạn");
                return response;
            }

            // Update new password
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            account.Key = null;
            accountRepo.Update(account);
            await unitOfWork.SaveChangesAsync(account.Email, cancellationToken);

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Đặt lại mật khẩu");
            return response;
        }
        catch (Exception e)
        {
            response.SetMessage(MessageId.E00000, "Link không hợp lệ hoặc đã hết hạn");
            return response;
        }
    }
}