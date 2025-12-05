using AuthService.Domain.WriteModels;
using BaseService.Application.Interfaces.Commons;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;

namespace AuthService.Application.Accounts.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler(ICommandRepository<Account> accountRepository, ICommonLogic commonLogic, IUnitOfWork unitOfWork) : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResponse>
    {
        public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var response = new ForgotPasswordResponse { Success = false };
            var account = await accountRepository
                .FirstOrDefaultAsync(
                predicate: a => a.Email == request.Email && a.IsActive,
                cancellationToken: cancellationToken);
            if (account == null)
            {
                response.SetMessage(MessageId.E00000, "Email không tồn tại trong hệ thống");
                return response;
            }

            string key = $"{DateTime.Now}-{account.Email}";

            // Endrypt key send to email
            var encryptedKey = commonLogic.EncryptText(key);

            // Publish event to UtilityService to send mail
            

            // Save change
            account.Key = encryptedKey.Response.EncryptedKey;
            accountRepository.Update(account);
            await unitOfWork.SaveChangesAsync(account.Email, cancellationToken);

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Đã gửi link xác nhận đến email của bạn");
            return response;
        }
    }
}
