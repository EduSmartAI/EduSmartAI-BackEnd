using System.ComponentModel.DataAnnotations;
using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace AuthService.Application.Accounts.Commands.UpdatePassword;

public class UpdatePasswordCommand : ICommand<UpdatePasswordResponse>
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; init; } = null!;
    
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải từ 6-100 ký tự")]
    public string NewPassword { get; init; } = null!;
    
    [Required(ErrorMessage = "Confirm password is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; init; } = null!;
}

public record UpdatePasswordResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}

