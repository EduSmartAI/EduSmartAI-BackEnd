using BaseService.Application.Interfaces.Repositories;
using MassTransit;

namespace AuthService.Application.Consumers;

public class AccountCollectionEventConsumer(IUnitOfWork unitOfWork) : IConsumer<AccountCollectionEvent>
{
    public async Task Consume(ConsumeContext<AccountCollectionEvent> context)
    {
        var accountCollection = context.Message.Account;
        unitOfWork.Store(accountCollection);
        await unitOfWork.SessionSaveChangesAsync();
    }
}