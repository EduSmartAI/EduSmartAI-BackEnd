using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace AuthService.Application.Accounts.Commands.Inserts;

public record StudentInsertCommand : ICommand<StudentInsertResponse>
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    [DefaultValue("edusmartAI@gmail.com")]
    public string Email { get; init; } = null!;
    
    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-100 ký tự")]
    [DefaultValue("Edusmart@123")]
    public string Password { get; init; } = null!;
    
    [Required(ErrorMessage = "Họ và tên đệm là bắt buộc")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Họ và tên đệm phải từ 2-50 ký tự")]
    [RegularExpression(@"^[\p{L}\s]+$", ErrorMessage = "Họ và Tên đệm chỉ được chứa chữ cái và khoảng trắng")]  
    [DefaultValue("Edu")]
    public string FirstName { get; init; } = null!;

    [Required(ErrorMessage = "Tên là bắt buộc")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Tên phải từ 2-50 ký tự")]
    [RegularExpression(@"^[\p{L}\s]+$", ErrorMessage = "Tên chỉ được chứa chữ cái và khoảng trắng")]  
    [DefaultValue("Smárt")]
    public string LastName { get; init; } = null!;}