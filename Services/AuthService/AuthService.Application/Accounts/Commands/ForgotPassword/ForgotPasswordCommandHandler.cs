using AuthService.Domain.WriteModels;
using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.CQRS;

namespace AuthService.Application.Accounts.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler(ICommandRepository<Account> accountRepository) : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResponse>
    {
        public Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
