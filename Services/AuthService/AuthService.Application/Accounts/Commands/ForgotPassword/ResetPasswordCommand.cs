using System.ComponentModel.DataAnnotations;
using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace AuthService.Application.Accounts.Commands.ForgotPassword;

public class ResetPasswordCommand : ICommand<ResetPasswordResponse>
{
    [Required(ErrorMessage = "Key is required")]
    public string Key { get; init; } = null!;
    
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-100 ký tự")]
    public string NewPassword { get; init; } = null!;
}

public record ResetPasswordResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}