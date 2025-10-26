using AuthService.Domain.ReadModels;

namespace AuthService.Application.Consumers;

public class AccountCollectionEvent
{
    public AccountCollection Account { get; set; } = null!;
}