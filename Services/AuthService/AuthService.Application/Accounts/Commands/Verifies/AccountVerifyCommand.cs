using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace AuthService.Application.Accounts.Commands.Verifies;

public record AccountVerifyCommand : ICommand<AccountVerifyResponse>
{
    [Required(ErrorMessage = "Key is required")]
    public string Key { get; init; } = null!;
}